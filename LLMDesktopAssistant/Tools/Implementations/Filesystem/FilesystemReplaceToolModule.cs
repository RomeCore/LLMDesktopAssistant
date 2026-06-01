using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils.Files;

namespace LLMDesktopAssistant.Tools.Implementations.Filesystem
{
	[ToolModule]
	public class FilesystemReplaceToolModule : ToolModule
	{
		private readonly FileAccessService _fileAccess;

		public FilesystemReplaceToolModule(FileAccessService fileAccess)
		{
			_fileAccess = fileAccess;

			AddTool(ReplaceInFile,
				new ToolInitializationInfo
				{
					Name = "fs-replace",
					Description = """
						Replaces all occurrences of a string or regex pattern in a text file.
						Returns detailed information about applied changes.
						""",
					Category = "filesystem",
					AskForConfirmation = true
				});
		}

		public ReactiveToolResult ReplaceInFile(
			string path,
			[Description("The string to search for. If null or empty, the 'oldRegex' must be provided.")]
			string? oldString = null,
			[Description("The regex to search for. If null or empty, the 'oldString' must be provided.")]
			string? oldRegex = null,
			[Description("The replacement string. If null, the oldString or oldRegex will be removed (replaced with empty string).")]
			string? newString = null,
			[Description("If true, leading/trailing whitespace and line ending differences are ignored when matching. Only works with 'oldString' (not regex). Applies dedent to multi-line strings.")]
			bool ignoreWhitespaceDifferences = false)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);
				var fileName = Path.GetFileName(fullPath);

				if (!File.Exists(fullPath))
					return ReactiveToolResult.CreateError("File not found.");

				if (FileUtils.IsBinaryFile(fullPath))
					return ReactiveToolResult.CreateError("Cannot replace text in binary files.");

				if (string.IsNullOrEmpty(oldString) && string.IsNullOrEmpty(oldRegex))
					return ReactiveToolResult.CreateError("Both 'oldString' and 'oldRegex' parameters cannot be null or empty.");

				var originalContent = File.ReadAllText(fullPath);

				// If using oldString with ignoreWhitespaceDifferences, use line-based matching
				if (oldString != null && ignoreWhitespaceDifferences)
				{
					return ReplaceWithWhitespaceIgnored(fullPath, fileName, oldString, newString ?? string.Empty, originalContent);
				}

				var regex = oldString != null ?
					new Regex(Regex.Escape(oldString), RegexOptions.Compiled) :
					new Regex(oldRegex!, RegexOptions.Compiled);
				newString ??= string.Empty;

				var matches = regex.Matches(originalContent);
				var totalReplacements = matches.Count;

				if (totalReplacements == 0)
				{
					var noChangeResult = new ReactiveToolResult
					{
						StatusIcon = Material.Icons.MaterialIconKind.Information,
						StatusTitle = $"**{fileName}** *({LocalizationManager.LocalizeStatic("fs-changes_none")})*",
						ResultContent = $"No occurrences of '{oldString ?? oldRegex}' found in file '{path}'."
					};
					return noChangeResult.Complete(true);
				}

				var newContent = regex.Replace(originalContent, newString);
				File.WriteAllText(fullPath, newContent);

				var report = new StringBuilder();
				var oldPattern = oldString ?? oldRegex!;
				report.AppendLine($"Successfully replaced '{oldPattern}' with '{newString ?? "(empty)"}' in file '{path}'");
				report.AppendLine($"Summary:");
				report.AppendLine($"  Total replacements: {totalReplacements}");
				report.AppendLine($"  File size before: {originalContent.Length} bytes");
				report.AppendLine($"  File size after: {newContent.Length} bytes");

				if (totalReplacements <= 10) // Show all replacements if 10 or less
				{
					report.AppendLine();
					report.AppendLine($"Replacement details:");
					for (int i = 0; i < matches.Count; i++)
					{
						var match = matches[i];
						var contextStart = Math.Max(0, match.Index - 20);
						var contextEnd = Math.Min(originalContent.Length, match.Index + match.Length + 20);
						var context = originalContent.Substring(contextStart, contextEnd - contextStart);

						report.AppendLine($"  Match {i + 1}:");
						report.AppendLine($"    Position: {match.Index}");
						report.AppendLine($"    Context: ...{Truncate(context, 80)}...");
						report.AppendLine();
					}
				}
				else // Show only first 5 and last 5 replacements
				{
					report.AppendLine();
					report.AppendLine($"First 5 replacements:");
					for (int i = 0; i < Math.Min(5, totalReplacements); i++)
					{
						var match = matches[i];
						var contextStart = Math.Max(0, match.Index - 20);
						var contextEnd = Math.Min(originalContent.Length, match.Index + match.Length + 20);
						var context = originalContent.Substring(contextStart, contextEnd - contextStart);

						report.AppendLine($"  Match {i + 1} (position {match.Index}): ...{Truncate(context, 60)}...");
					}

					if (totalReplacements > 10)
					{
						report.AppendLine($"   ... and {totalReplacements - 10} more replacements");
					}

					if (totalReplacements > 5)
					{
						report.AppendLine($"Last 5 replacements:");
						for (int i = Math.Max(5, totalReplacements - 5); i < totalReplacements; i++)
						{
							var match = matches[i];
							var contextStart = Math.Max(0, match.Index - 20);
							var contextEnd = Math.Min(originalContent.Length, match.Index + match.Length + 20);
							var context = originalContent.Substring(contextStart, contextEnd - contextStart);

							report.AppendLine($"  Match {i + 1} (position {match.Index}): ...{Truncate(context, 60)}...");
						}
					}
				}

				var changeDescription = string.Format(
					LocalizationManager.LocalizeStatic("fs-changes_text_replaced"),
					totalReplacements);

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.FileDocumentEdit,
					StatusTitle = $"**{fileName}** *({changeDescription})*",
					ResultContent = report.ToString()
				};

				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error replacing text in file: {ex.Message}");
			}
		}

		private ReactiveToolResult ReplaceWithWhitespaceIgnored(
			string fullPath,
			string fileName,
			string oldString,
			string newString,
			string originalContent)
		{
			// Normalize line endings
			static string Normalize(string text) => text.Replace("\r\n", "\n").Replace("\r", "\n");

			var normalizedContent = Normalize(originalContent);
			var normalizedOld = Normalize(oldString);
			var normalizedNew = Normalize(newString);

			var fileLines = normalizedContent.Split('\n').ToList();
			var oldLines = normalizedOld.Split('\n').ToList();
			var newLines = normalizedNew.Split('\n').ToList();

			// Dedent oldLines: remove common leading whitespace
			var nonEmptyOld = oldLines.Where(l => l.Trim().Length > 0).ToList();
			if (nonEmptyOld.Count > 0)
			{
				int minIndent = nonEmptyOld.Min(l => l.Length - l.TrimStart().Length);
				if (minIndent > 0)
				{
					var indent = new string(' ', minIndent);
					oldLines = oldLines.Select(l => l.StartsWith(indent) ? l[minIndent..] : l).ToList();
				}
			}

			// Trim old lines for comparison
			var trimmedOld = oldLines.Select(l => l.Trim()).ToList();

			// Find all occurrences
			var matchIndices = new List<int>();
			for (int i = 0; i <= fileLines.Count - oldLines.Count; i++)
			{
				bool found = true;
				for (int j = 0; j < oldLines.Count; j++)
				{
					if (fileLines[i + j].Trim() != trimmedOld[j])
					{
						found = false;
						break;
					}
				}
				if (found)
				{
					matchIndices.Add(i);
				}
			}

			if (matchIndices.Count == 0)
			{
				var noChangeResult = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.Information,
					StatusTitle = $"**{fileName}** *({LocalizationManager.LocalizeStatic("fs-changes_none")})*",
					ResultContent = $"No occurrences of '{oldString}' found in file '{fullPath}' (whitespace-insensitive search)."
				};
				return noChangeResult.Complete(true);
			}

			// Apply replacements from end to start
			foreach (var idx in matchIndices.OrderByDescending(i => i))
			{
				fileLines.RemoveRange(idx, oldLines.Count);
				fileLines.InsertRange(idx, newLines);
			}

			var newContent = string.Join("\n", fileLines);

			// Preserve original line endings if they were \r\n
			if (originalContent.Contains("\r\n") && !newContent.Contains("\r\n"))
			{
				newContent = newContent.Replace("\n", "\r\n");
			}

			File.WriteAllText(fullPath, newContent);

			var report = new StringBuilder();
			report.AppendLine($"Replaced '{oldString}' with '{newString}' in file '{fileName}' (whitespace-insensitive)");
			report.AppendLine($"Summary:");
			report.AppendLine($"  Total replacements: {matchIndices.Count}");
			report.AppendLine($"  Lines per match: {oldLines.Count} → {newLines.Count}");
			report.AppendLine($"  File size before: {originalContent.Length} bytes");
			report.AppendLine($"  File size after: {newContent.Length} bytes");

			var changeDescription = string.Format(
				LocalizationManager.LocalizeStatic("fs-changes_text_replaced"),
				matchIndices.Count);

			var result = new ReactiveToolResult
			{
				StatusIcon = Material.Icons.MaterialIconKind.FileDocumentEdit,
				StatusTitle = $"**{fileName}** *({changeDescription})*",
				ResultContent = report.ToString()
			};

			return result.Complete(true);
		}

		private string Truncate(string text, int maxLength)
		{
			if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
				return text;

			return text.Substring(0, maxLength - 3) + "...";
		}
	}
}
