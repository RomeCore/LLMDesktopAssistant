using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils.Files;

namespace LLMDesktopAssistant.Tools.Implementations.Filesystem
{
	/// <summary>
	/// Universal file editing tool. Supports context-based editing (by string content)
	/// and regex-based editing. Combines capabilities of the old fs-replace and fs-edit.
	/// </summary>
	[ToolModule]
	public class FilesystemEditToolModule : ToolModule
	{
		private readonly FileAccessService _fileAccess;

		public FilesystemEditToolModule(FileAccessService fileAccess)
		{
			_fileAccess = fileAccess;

			AddTool(Edit,
				new ToolInitializationInfo
				{
					Name = "fs-edit",
					Description = """
						Edits a text file by finding a context and performing an operation on it.
						Supports both plain text (with flexible whitespace matching) and regex patterns.

						Available operations:
						- `replace`       — replaces all occurrences of `match` with `text`
						- `insert_before` — inserts `text` BEFORE every occurrence of `match`
						- `insert_after`  — inserts `text` AFTER every occurrence of `match`
						- `delete`        — deletes every occurrence of `match`

						When `useRegex = false` (default — plain text mode):
						- `match` is treated as a literal string (can be multi-line)
						- Leading/trailing whitespace and line endings are ignored in matching
						- Common indentation in multi-line `match` is automatically stripped (dedent)
						- So `match: "public class Foo"` will match `"    public class Foo"` in the file

						When `useRegex = true` (regex mode):
						- `match` is interpreted as a .NET regular expression
						- For `replace` operation: standard regex replacement with capture groups support
						- For `insert_before`/`insert_after`/`delete`: lines containing the regex match are targeted
						- `ignoreWhitespace` is not applied in regex mode

						Examples:
						- Replace text (plain, whitespace-insensitive):
						  fs-edit(path: "file.cs", match: "public class Foo", operation: "replace", text: "public class Bar")
						- Insert attribute before a method:
						  fs-edit(path: "MyClass.cs", match: "void MyMethod()", operation: "insert_before", text: "[MyAttribute]")
						- Add line after XML element:
						  fs-edit(path: "config.xml", match: "<value key=\"x\"></value>", operation: "insert_after", text: "<data>new</data>")
						- Regex replace (rename all classes):
						  fs-edit(path: "file.cs", match: "class (\w+)", operation: "replace", text: "class Renamed_$1", useRegex: true)
						- Regex delete (remove debug lines):
						  fs-edit(path: "file.cs", match: "Console\.WriteLine", operation: "delete", useRegex: true)
						""",
					Category = "filesystem",
					AskForConfirmation = true
				});
		}

		public ReactiveToolResult Edit(
			[Description("The path to the file to edit.")]
			string path,
			[Description("The text or regex pattern to search for. Can include multiple lines in plain text mode.")]
			string match,
			[Description("The operation: 'replace', 'insert_before', 'insert_after', or 'delete'.")]
			string operation,
			[Description("The replacement text (for 'replace'/'insert_before'/'insert_after') or empty for 'delete'.")]
			string text = "",
			[Description("If true, 'match' is treated as a .NET regular expression instead of plain text.")]
			bool useRegex = false,
			[Description("Which occurrence to act on: 0 = all, 1 = first, 2 = second, etc.")]
			int occurrence = 0,
			[Description("If true (plain text mode only), leading/trailing whitespace and line endings are ignored. Dedent is applied.")]
			bool ignoreWhitespace = true,
			[Description("If true, case is ignored when matching.")]
			bool ignoreCase = false,
			CancellationToken cancellationToken = default)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);
				var fileName = Path.GetFileName(fullPath);

				if (!File.Exists(fullPath))
					return ReactiveToolResult.CreateError("File not found.");

				if (FileUtils.IsBinaryFile(fullPath))
					return ReactiveToolResult.CreateError("Cannot edit binary files.");

				if (string.IsNullOrWhiteSpace(match))
					return ReactiveToolResult.CreateError("'match' parameter cannot be empty.");

				if (operation is not ("replace" or "insert_before" or "insert_after" or "delete"))
					return ReactiveToolResult.CreateError("'operation' must be one of: 'replace', 'insert_before', 'insert_after', 'delete'.");

				var originalContent = File.ReadAllText(fullPath);
				var normalizedContent = NormalizeLineEndings(originalContent);
				var fileLines = normalizedContent.Split('\n').ToList();

				if (useRegex)
				{
					return EditWithRegex(fullPath, fileName, path, match, text, operation, occurrence, ignoreCase, originalContent, normalizedContent, fileLines, cancellationToken);
				}
				else
				{
					return EditWithString(fullPath, fileName, path, match, text, operation, occurrence, ignoreWhitespace, ignoreCase, originalContent, normalizedContent, fileLines, cancellationToken);
				}
			}
			catch (OperationCanceledException)
			{
				return ReactiveToolResult.CreateError("Edit operation was cancelled.");
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error editing file: {ex.Message}");
			}
		}

		#region Plain-Text Mode

		private ReactiveToolResult EditWithString(
			string fullPath,
			string fileName,
			string path,
			string match,
			string text,
			string operation,
			int occurrence,
			bool ignoreWhitespace,
			bool ignoreCase,
			string originalContent,
			string normalizedContent,
			List<string> fileLines,
			CancellationToken cancellationToken)
		{
			var normalizedMatch = NormalizeLineEndings(match);
			var matchLines = normalizedMatch.Split('\n').ToList();
			if (matchLines.Count > 0 && string.IsNullOrEmpty(matchLines[^1]))
				matchLines.RemoveAt(matchLines.Count - 1); // Remove trailing empty line if present (LLMs loves to add them!)

			if (ignoreWhitespace)
			{
				// Apply dedent: remove common leading whitespace
				matchLines = DedentLines(matchLines);
				// Also trim each line for comparison
				matchLines = matchLines.Select(l => l.Trim()).ToList();
			}

			var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

			// Find all occurrences of matchLines in fileLines
			var foundIndices = FindSequenceIndices(fileLines, matchLines, ignoreWhitespace, comparison, cancellationToken);

			if (foundIndices.Count == 0)
			{
				var noChangeResult = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.Information,
					StatusTitle = $"**{fileName}** *({LocalizationManager.LocalizeStatic("fs-changes_none")})*",
					ResultContent = $"No occurrences of the specified context were found."
				};
				return noChangeResult.Complete(true);
			}

			// Filter by occurrence
			var targetIndices = FilterOccurrences(foundIndices, occurrence);
			if (targetIndices == null)
			{
				return ReactiveToolResult.CreateError(
					$"Occurrence {occurrence} not found. Only {foundIndices.Count} occurrence(s) exist.");
			}

			// Apply operations
			var operationLines = string.IsNullOrEmpty(text)
				? new List<string>()
				: NormalizeLineEndings(text).Split('\n').ToList();

			var (totalInsertions, totalDeletions, _) = ApplyLineOperations(fileLines, targetIndices, matchLines.Count, operationLines, operation);

			var newContent = string.Join("\n", fileLines);

			// Preserve original line endings
			newContent = PreserveLineEndings(originalContent, newContent);

			if (newContent == originalContent)
			{
				var noChangeResult = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.Information,
					StatusTitle = $"**{fileName}** *({LocalizationManager.LocalizeStatic("fs-changes_none")})*",
					ResultContent = "No changes were made (content is identical)."
				};
				return noChangeResult.Complete(true);
			}

			File.WriteAllText(fullPath, newContent);

			return BuildSuccessResult(fileName, path, operation, foundIndices.Count, targetIndices.Count,
				matchLines.Count, operationLines.Count, totalDeletions, totalInsertions,
				originalContent.Length, newContent.Length, match);
		}

		#endregion

		#region Regex Mode

		private ReactiveToolResult EditWithRegex(
			string fullPath,
			string fileName,
			string path,
			string pattern,
			string text,
			string operation,
			int occurrence,
			bool ignoreCase,
			string originalContent,
			string normalizedContent,
			List<string> fileLines,
			CancellationToken cancellationToken)
		{
			var regexOptions = RegexOptions.Compiled | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
			var regex = new Regex(pattern, regexOptions);
			text ??= string.Empty;

			if (operation == "replace")
			{
				// Standard regex replace on the whole content
				var matches = regex.Matches(normalizedContent);
				var totalReplacements = matches.Count;

				if (totalReplacements == 0)
				{
					var noChangeResult = new ReactiveToolResult
					{
						StatusIcon = Material.Icons.MaterialIconKind.Information,
						StatusTitle = $"**{fileName}** *({LocalizationManager.LocalizeStatic("fs-changes_none")})*",
						ResultContent = $"No matches for pattern '{pattern}' found."
					};
					return noChangeResult.Complete(true);
				}

				int count = 0;
				var newContent = regex.Replace(normalizedContent, m => ++count == occurrence ? text : m.Value);
				newContent = PreserveLineEndings(originalContent, newContent);

				File.WriteAllText(fullPath, newContent);

				var report = new StringBuilder();
				report.AppendLine($"Regex replacement in '{fileName}':");
				report.AppendLine($"  Pattern: '{pattern}'");
				report.AppendLine($"  Replacement: '{text}'");
				report.AppendLine($"  Total replacements: {totalReplacements}");
				report.AppendLine($"  File size before: {originalContent.Length} bytes");
				report.AppendLine($"  File size after: {newContent.Length} bytes");

				if (totalReplacements <= 10)
				{
					report.AppendLine();
					report.AppendLine("Replacement details:");
					for (int i = 0; i < matches.Count; i++)
					{
						var match = matches[i];
						var contextStart = Math.Max(0, match.Index - 20);
						var contextEnd = Math.Min(normalizedContent.Length, match.Index + match.Length + 20);
						var context = normalizedContent.Substring(contextStart, contextEnd - contextStart);

						report.AppendLine($"  Match {i + 1}:");
						report.AppendLine($"    Position: {match.Index}");
						report.AppendLine($"    Context: ...{Truncate(context, 80)}...");
						report.AppendLine();
					}
				}
				else
				{
					report.AppendLine();
					report.AppendLine($"First 5 replacements:");
					for (int i = 0; i < Math.Min(5, totalReplacements); i++)
					{
						var match = matches[i];
						var contextStart = Math.Max(0, match.Index - 20);
						var contextEnd = Math.Min(normalizedContent.Length, match.Index + match.Length + 20);
						var context = normalizedContent.Substring(contextStart, contextEnd - contextStart);

						report.AppendLine($"  Match {i + 1} (position {match.Index}): ...{Truncate(context, 60)}...");
					}

					if (totalReplacements > 10)
						report.AppendLine($"   ... and {totalReplacements - 10} more replacements");

					if (totalReplacements > 5)
					{
						report.AppendLine("Last 5 replacements:");
						for (int i = Math.Max(5, totalReplacements - 5); i < totalReplacements; i++)
						{
							var match = matches[i];
							var contextStart = Math.Max(0, match.Index - 20);
							var contextEnd = Math.Min(normalizedContent.Length, match.Index + match.Length + 20);
							var context = normalizedContent.Substring(contextStart, contextEnd - contextStart);

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
			else
			{
				// insert_before / insert_after / delete on lines that contain regex match
				var matchedLineIndices = new List<int>();
				for (int i = 0; i < fileLines.Count; i++)
				{
					cancellationToken.ThrowIfCancellationRequested();
					if (regex.IsMatch(fileLines[i]))
					{
						matchedLineIndices.Add(i);
					}
				}

				if (matchedLineIndices.Count == 0)
				{
					var noChangeResult = new ReactiveToolResult
					{
						StatusIcon = Material.Icons.MaterialIconKind.Information,
						StatusTitle = $"**{fileName}** *({LocalizationManager.LocalizeStatic("fs-changes_none")})*",
						ResultContent = $"No lines matched pattern '{pattern}'."
					};
					return noChangeResult.Complete(true);
				}

				var targetIndices = FilterOccurrences(matchedLineIndices, occurrence);
				if (targetIndices == null)
				{
					return ReactiveToolResult.CreateError(
						$"Occurrence {occurrence} not found. Only {matchedLineIndices.Count} occurrence(s) exist.");
				}

				var operationLines = string.IsNullOrEmpty(text)
					? new List<string>()
					: NormalizeLineEndings(text).Split('\n').ToList();

				// For line-based operations, match length is 1 (single line)
				var (totalInsertions, totalDeletions, _) = ApplyLineOperations(fileLines, targetIndices, 1, operationLines, operation);

				var newContent = string.Join("\n", fileLines);
				newContent = PreserveLineEndings(originalContent, newContent);

				if (newContent == originalContent)
				{
					var noChangeResult = new ReactiveToolResult
					{
						StatusIcon = Material.Icons.MaterialIconKind.Information,
						StatusTitle = $"**{fileName}** *({LocalizationManager.LocalizeStatic("fs-changes_none")})*",
						ResultContent = "No changes were made."
					};
					return noChangeResult.Complete(true);
				}

				File.WriteAllText(fullPath, newContent);

				return BuildSuccessResult(fileName, path, $"{operation} (regex, line-based)", matchedLineIndices.Count, targetIndices.Count,
					1, operationLines.Count, totalDeletions, totalInsertions,
					originalContent.Length, newContent.Length, pattern);
			}
		}

		#endregion

		#region Shared Helpers

		private static string NormalizeLineEndings(string text)
			=> text.Replace("\r\n", "\n").Replace("\r", "\n");

		private static string PreserveLineEndings(string original, string modified)
		{
			if (original.Contains("\r\n") && !modified.Contains("\r\n"))
				return modified.Replace("\n", "\r\n");
			return modified;
		}

		private static List<string> DedentLines(List<string> lines)
		{
			var nonEmpty = lines.Where(l => l.Trim().Length > 0).ToList();
			if (nonEmpty.Count == 0)
				return lines;

			// Find the longest common leading whitespace prefix across all non-empty lines.
			// Supports both spaces and tabs, and mixed indentation.
			int maxLookup = nonEmpty.Min(l => l.Length);
			int commonLen = 0;
			for (int i = 0; i < maxLookup; i++)
			{
				char c = nonEmpty[0][i];
				if (c is not ' ' and not '\t')
					break;

				// All non-empty lines must have the same character at this position
				if (!nonEmpty.All(l => l[i] == c))
					break;

				commonLen = i + 1;
			}

			if (commonLen == 0)
				return lines;

			var prefix = nonEmpty[0][..commonLen];
			return lines.Select(l => l.StartsWith(prefix) ? l[commonLen..] : l).ToList();
		}

		private static List<int> FindSequenceIndices(
			List<string> fileLines,
			List<string> matchLines,
			bool ignoreWhitespace,
			StringComparison comparison,
			CancellationToken cancellationToken)
		{
			var indices = new List<int>();
			if (matchLines.Count == 0)
				return indices;

			var trimmedMatch = ignoreWhitespace
				? matchLines.Select(l => l.Trim()).ToList()
				: matchLines;

			for (int i = 0; i <= fileLines.Count - matchLines.Count; i++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				bool found = true;
				for (int j = 0; j < matchLines.Count; j++)
				{
					var fileLine = ignoreWhitespace ? fileLines[i + j].Trim() : fileLines[i + j];
					if (!string.Equals(fileLine, trimmedMatch[j], comparison))
					{
						found = false;
						break;
					}
				}

				if (found)
					indices.Add(i);
			}

			return indices;
		}

		private static List<int>? FilterOccurrences(List<int> found, int occurrence)
		{
			if (occurrence > 0)
			{
				if (occurrence > found.Count)
					return null;
				return [found[occurrence - 1]];
			}
			return found;
		}

		private static (int totalInsertions, int totalDeletions, List<string> finalLines) ApplyLineOperations(
			List<string> fileLines,
			List<int> targetIndices,
			int matchLength,
			List<string> operationLines,
			string operation)
		{
			int totalInsertions = 0;
			int totalDeletions = 0;

			// Apply from end to preserve indices
			foreach (var idx in targetIndices.OrderByDescending(i => i))
			{
				switch (operation)
				{
					case "insert_before":
						fileLines.InsertRange(idx, operationLines);
						totalInsertions += operationLines.Count;
						break;

					case "insert_after":
						fileLines.InsertRange(idx + matchLength, operationLines);
						totalInsertions += operationLines.Count;
						break;

					case "replace":
						fileLines.RemoveRange(idx, matchLength);
						fileLines.InsertRange(idx, operationLines);
						totalDeletions += matchLength;
						totalInsertions += operationLines.Count;
						break;

					case "delete":
						fileLines.RemoveRange(idx, matchLength);
						totalDeletions += matchLength;
						break;
				}
			}

			return (totalInsertions, totalDeletions, fileLines);
		}

		private static ReactiveToolResult BuildSuccessResult(
			string fileName,
			string path,
			string operation,
			int totalFound,
			int totalAffected,
			int matchLineCount,
			int operationLineCount,
			int totalDeletions,
			int totalInsertions,
			int originalSize,
			int newSize,
			string pattern)
		{
			var report = new StringBuilder();
			report.AppendLine($"Applied '{operation}' on '{fileName}':");
			report.AppendLine($"  Occurrences affected: {totalAffected} (out of {totalFound} found)");
			if (totalDeletions > 0)
				report.AppendLine($"  Lines removed: {totalDeletions}");
			if (totalInsertions > 0 && operation != "replace")
				report.AppendLine($"  Lines inserted: {totalInsertions}");
			if (operation == "replace")
				report.AppendLine($"  Lines replaced: {matchLineCount} → {operationLineCount} (per occurrence)");
			report.AppendLine($"  File size before: {originalSize} bytes");
			report.AppendLine($"  File size after: {newSize} bytes");

			report.AppendLine();
			report.AppendLine("Change preview:");
			var displayPattern = pattern.Length > 80 ? pattern[..77] + "..." : pattern;
			report.AppendLine($"  Match: '{displayPattern}'");

			var changeDescription = string.Format(
				LocalizationManager.LocalizeStatic("fs-edit_changes_count"),
				totalAffected, operation);

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

		#endregion
	}
}
