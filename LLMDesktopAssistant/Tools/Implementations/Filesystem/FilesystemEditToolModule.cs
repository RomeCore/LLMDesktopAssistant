using System.ComponentModel;
using System.Text;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils.Files;

namespace LLMDesktopAssistant.Tools.Implementations.Filesystem
{
	/// <summary>
	/// Tool module that provides context-based file editing (insert before/after, replace, delete by content).
	/// Unlike fs-apply_diff (which works by line numbers) and fs-replace (regex-based),
	/// this tool finds the context by matching the actual text content,
	/// with support for ignoring whitespace differences.
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
						Edits a file by finding a context string and performing an operation relative to it.
						Unlike fs-apply_diff (line-number-based) and fs-replace (regex-based),
						this tool finds text by its actual content, supporting flexible whitespace matching.

						Operations:
						- `insert_before` — inserts `text` BEFORE every occurrence of `match`
						- `insert_after`  — inserts `text` AFTER every occurrence of `match`
						- `replace`       — replaces every occurrence of `match` with `text`
						- `delete`        — deletes every occurrence of `match`

						When `ignoreWhitespace = true` (default):
						- Leading/trailing whitespace is ignored when comparing lines
						- Common indentation in multi-line `match` is automatically stripped (dedent)
						- Line endings (\r\n vs \n) are normalized
						This means `match: "public class Foo"` will match `"    public class Foo"` in the file.

						Examples:
						- Insert attribute before a method:
						  fs-edit(path: "MyClass.cs", match: "public void MyMethod()", operation: "insert_before", text: "[MyAttribute]")
						- Add line after a specific XML element:
						  fs-edit(path: "config.xml", match: "<value key=\"x\"></value>", operation: "insert_after", text: "<data>new</data>")
						- Replace a multi-line block ignoring indentation:
						  fs-edit(path: "Program.cs", match: "public class Foo\n{\n}", operation: "replace", text: "public class Bar\n{\n}")
						- Delete specific line:
						  fs-edit(path: "file.txt", match: "obsolete line", operation: "delete")
						""",
					Category = "filesystem",
					AskForConfirmation = true
				});
		}

		public ReactiveToolResult Edit(
			[Description("The path to the file to edit.")]
			string path,
			[Description("The context text to search for. Can include multiple lines.")]
			string match,
			[Description("The operation to perform: 'insert_before', 'insert_after', 'replace', or 'delete'.")]
			string operation,
			[Description("The text to insert or replace with. Not used for 'delete' operation.")]
			string text = "",
			[Description("Which occurrence to act on: 0 = all, 1 = first, 2 = second, etc.")]
			int occurrence = 0,
			[Description("If true, leading/trailing whitespace and line ending differences are ignored when matching. Dedent is applied to multi-line match.")]
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

				if (operation is not ("insert_before" or "insert_after" or "replace" or "delete"))
					return ReactiveToolResult.CreateError("'operation' must be one of: 'insert_before', 'insert_after', 'replace', 'delete'.");

				if (operation == "delete" && !string.IsNullOrEmpty(text))
					text = ""; // text is not used for delete

				var originalContent = File.ReadAllText(fullPath);
				var normalizedContent = NormalizeLineEndings(originalContent);
				var fileLines = normalizedContent.Split('\n').ToList();

				// Normalize and prepare match lines
				var normalizedMatch = NormalizeLineEndings(match);
				var matchLines = normalizedMatch.Split('\n').ToList();

				if (ignoreWhitespace)
				{
					// Apply dedent: remove common leading whitespace from match
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
						ResultContent = $"No occurrences of the specified context were found in file '{path}'."
					};
					return noChangeResult.Complete(true);
				}

				// Filter by occurrence
				List<int> targetIndices;
				if (occurrence > 0)
				{
					if (occurrence > foundIndices.Count)
					{
						return ReactiveToolResult.CreateError(
							$"Occurrence {occurrence} not found. Only {foundIndices.Count} occurrence(s) exist.");
					}
					targetIndices = [foundIndices[occurrence - 1]];
				}
				else
				{
					targetIndices = foundIndices;
				}

				// Apply operations (from end to start to preserve indices)
				var operationLines = string.IsNullOrEmpty(text)
					? new List<string>()
					: NormalizeLineEndings(text).Split('\n').ToList();

				var totalInsertions = 0;
				var totalDeletions = 0;

				// Sort in descending order to apply from end
				foreach (var idx in targetIndices.OrderByDescending(i => i))
				{
					cancellationToken.ThrowIfCancellationRequested();

					switch (operation)
					{
						case "insert_before":
							fileLines.InsertRange(idx, operationLines);
							totalInsertions += operationLines.Count;
							break;

						case "insert_after":
							fileLines.InsertRange(idx + matchLines.Count, operationLines);
							totalInsertions += operationLines.Count;
							break;

						case "replace":
							fileLines.RemoveRange(idx, matchLines.Count);
							fileLines.InsertRange(idx, operationLines);
							totalDeletions += matchLines.Count;
							totalInsertions += operationLines.Count;
							break;

						case "delete":
							fileLines.RemoveRange(idx, matchLines.Count);
							totalDeletions += matchLines.Count;
							break;
					}
				}

				var newContent = string.Join("\n", fileLines);
				if (newContent == normalizedContent)
				{
					var noChangeResult = new ReactiveToolResult
					{
						StatusIcon = Material.Icons.MaterialIconKind.Information,
						StatusTitle = $"**{fileName}** *({LocalizationManager.LocalizeStatic("fs-changes_none")})*",
						ResultContent = "No changes were made (content is identical)."
					};
					return noChangeResult.Complete(true);
				}

				// Preserve original line endings if all were \r\n
				if (originalContent.Contains("\r\n") && !newContent.Contains("\r\n"))
				{
					newContent = newContent.Replace("\n", "\r\n");
				}

				File.WriteAllText(fullPath, newContent);

				var report = new StringBuilder();
				report.AppendLine($"Applied '{operation}' on '{fileName}':");
				report.AppendLine($"  Occurrences affected: {targetIndices.Count} (out of {foundIndices.Count} found)");
				if (totalDeletions > 0)
					report.AppendLine($"  Lines removed: {totalDeletions}");
				if (totalInsertions > 0 && operation != "replace")
					report.AppendLine($"  Lines inserted: {totalInsertions}");
				if (operation == "replace")
				{
					report.AppendLine($"  Lines replaced: {matchLines.Count} → {operationLines.Count} (per occurrence)");
				}
				report.AppendLine($"  File size before: {originalContent.Length} bytes");
				report.AppendLine($"  File size after: {newContent.Length} bytes");

				// Show context of changes
				report.AppendLine();
				report.AppendLine("Change preview:");
				if (matchLines.Count <= 5)
				{
					report.AppendLine($"  Match: '{string.Join("\\n", matchLines)}'");
				}
				else
				{
					report.AppendLine($"  Match: '{string.Join("\\n", matchLines.Take(2))}' ... ({matchLines.Count} lines total)");
				}

				var changeDescription = targetIndices.Count == 1
					? $"1 change ({operation})"
					: $"{targetIndices.Count} changes ({operation})";

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.FileDocumentEdit,
					StatusTitle = $"**{fileName}** *({changeDescription})*",
					ResultContent = report.ToString()
				};

				return result.Complete(true);
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

		#region Helpers

		/// <summary>
		/// Normalizes line endings to \n.
		/// </summary>
		private static string NormalizeLineEndings(string text)
		{
			return text.Replace("\r\n", "\n").Replace("\r", "\n");
		}

		/// <summary>
		/// Removes common leading whitespace from all non-empty lines.
		/// </summary>
		private static List<string> DedentLines(List<string> lines)
		{
			var nonEmpty = lines.Where(l => l.Trim().Length > 0).ToList();
			if (nonEmpty.Count == 0)
				return lines;

			int minIndent = nonEmpty.Min(l => l.Length - l.TrimStart().Length);
			if (minIndent == 0)
				return lines;

			var indent = new string(' ', minIndent);
			return lines.Select(l => l.StartsWith(indent) ? l[minIndent..] : l).ToList();
		}

		/// <summary>
		/// Finds all starting indices where the sequence of matchLines appears in fileLines.
		/// </summary>
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

			// Pre-compute trimmed match lines if needed
			var trimmedMatch = matchLines;
			if (ignoreWhitespace)
			{
				trimmedMatch = matchLines.Select(l => l.Trim()).ToList();
			}

			for (int i = 0; i <= fileLines.Count - matchLines.Count; i++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				bool found = true;
				for (int j = 0; j < matchLines.Count; j++)
				{
					var fileLine = ignoreWhitespace ? fileLines[i + j].Trim() : fileLines[i + j];
					var matchLine = trimmedMatch[j];

					if (!string.Equals(fileLine, matchLine, comparison))
					{
						found = false;
						break;
					}
				}

				if (found)
				{
					indices.Add(i);
				}
			}

			return indices;
		}

		#endregion
	}
}
