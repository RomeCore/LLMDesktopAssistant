using System.ComponentModel;
using System.Text.RegularExpressions;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils.Files;
using Material.Icons;

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

			AddTool(Edit, EditStreaming, EditPreview,
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
					DefaultExpectedBehaviour = ToolBehaviour.FileEdit
				});
		}

		private string? CheckArgs(string path, string? fullPath, string operation, bool useRegex, string match, string text)
		{
			if (fullPath == null)
				return $"Access outside working directory is not allowed: {path}";

			if (!File.Exists(fullPath))
				return $"File not found: {path}";

			if (string.IsNullOrWhiteSpace(match))
				return "'match' parameter cannot be empty.";

			if (operation is not ("replace" or "insert_before" or "insert_after" or "delete"))
				return "'operation' must be one of: 'replace', 'insert_before', 'insert_after', 'delete'.";

			return null;
		}

		public StreamingToolArgumentsAnalysisResult EditStreaming(
			string? path)
		{
			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.FileDocumentEdit,
				StatusTitle = $"**{path}**"
			};
		}

		public PreviewToolExecutionResult EditPreview(
			string path, string operation, bool useRegex, string match, string text = "",
			int occurrence = 0, bool ignoreWhitespace = true, bool ignoreCase = false,
			CancellationToken cancellationToken = default)
		{
			var fullPath = _fileAccess.TryAccessPath(path);
			var error = CheckArgs(path, fullPath, operation, useRegex, match, text);
			if (error != null)
			{
				return new PreviewToolExecutionResult
				{
					InterruptingSuccess = false,
					InterruptingContent = error,
					StatusIcon = MaterialIconKind.FileAlert,
					StatusTitle = $"**{path}**",
					ExpectedBehaviour = ToolBehaviour.None
				};
			}

			var originalContent = File.ReadAllText(fullPath!);
			var normalizedContent = NormalizeLineEndings(originalContent);
			var fileLines = normalizedContent.Split('\n').ToList();

			string? newContent, errorMessage;
			if (useRegex)
				(newContent, errorMessage) = EditWithRegex(match, text, operation, occurrence, ignoreCase, originalContent, normalizedContent, fileLines, cancellationToken);
			else
				(newContent, errorMessage) = EditWithString(match, text, operation, occurrence, ignoreWhitespace, ignoreCase, originalContent, normalizedContent, fileLines, cancellationToken);

			if (newContent == null || newContent == originalContent)
			{
				return new PreviewToolExecutionResult
				{
					InterruptingSuccess = true,
					InterruptingContent = errorMessage ?? "No changes were made to the file. The specified match was not found.",
					StatusIcon = MaterialIconKind.FileQuestion,
					StatusTitle = LocalizationManager.LocalizeStaticFormat("fs-edit_changes_applied_none", $"**{path}**"),
					ExpectedBehaviour = ToolBehaviour.None
				};
			}

			var diff = UnifiedDiff.Compute(originalContent, newContent, contextLines: 10);
			var (removed, added) = diff.GetChangeCounts();

			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.FileDocumentEdit,
				StatusTitle = $"**{path}** *(-{removed} +{added})*",
				ExpectedBehaviour = ToolBehaviour.FileEdit
			};
		}

		public ReactiveToolResult Edit(
			[Description("The path to the file to edit.")]
			string path,
			[Description("The operation: 'replace', 'insert_before', 'insert_after', or 'delete'.")]
			string operation,
			[Description("If true, 'match' is treated as a .NET regular expression instead of plain text.")]
			bool useRegex,
			[Description("The text or regex pattern to search for. Can include multiple lines in plain text mode.")]
			string match,
			[Description("The replacement text (for 'replace'/'insert_before'/'insert_after') or empty for 'delete'.")]
			string text = "",
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
				var fullPath = _fileAccess.TryAccessPath(path);
				var error = CheckArgs(path, fullPath, operation, useRegex, match, text);

				if (error != null)
				{
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.FileAlert,
						StatusTitle = $"**{path}**",
						ResultContent = error
					}.CompleteWithError();
				}

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

				string? newContent, errorMessage;
				if (useRegex)
					(newContent, errorMessage) = EditWithRegex(match, text, operation, occurrence, ignoreCase, originalContent, normalizedContent, fileLines, cancellationToken);
				else
					(newContent, errorMessage) = EditWithString(match, text, operation, occurrence, ignoreWhitespace, ignoreCase, originalContent, normalizedContent, fileLines, cancellationToken);

				if (newContent == null || newContent == originalContent)
				{
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.FileQuestion,
						StatusTitle = LocalizationManager.LocalizeStaticFormat("fs-edit_changes_applied_none", $"**{path}**"),
						ResultContent = errorMessage ?? "No changes were made to the file. The specified match was not found."
					}.CompleteWithSuccess();
				}

				File.WriteAllText(fullPath, newContent);

				var diff = UnifiedDiff.Compute(originalContent, newContent, contextLines: 10);
				var (removed, added) = diff.GetChangeCounts();

				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.FileDocumentEdit,
					StatusTitle = $"**{path}** *(-{removed} +{added})*",
					ResultContent = diff.ToString()
				}.CompleteWithSuccess();
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

		private (string?, string?) EditWithString(
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

			var foundIndices = FindSequenceIndices(fileLines, matchLines, ignoreWhitespace, comparison, cancellationToken);

			if (foundIndices.Count == 0)
				return (null, "No occurrences of the specified context were found.");

			var targetIndices = FilterOccurrences(foundIndices, occurrence);
			if (targetIndices == null)
				return (null, $"Occurrence {occurrence} not found. Only {foundIndices.Count} occurrence(s) exist.");

			// Apply operations
			var operationLines = string.IsNullOrEmpty(text)
				? new List<string>()
				: NormalizeLineEndings(text).Split('\n').ToList();

			var (totalInsertions, totalDeletions) = ApplyLineOperations(fileLines, targetIndices, matchLines.Count, operationLines, operation);

			var newContent = string.Join("\n", fileLines);

			// Preserve original line endings
			newContent = PreserveLineEndings(originalContent, newContent);

			if (newContent == originalContent)
				return (null, "No changes were made (content is identical).");

			return (newContent, null);
		}

		#endregion

		#region Regex Mode

		private (string?, string?) EditWithRegex(
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
					return (null, "No matches found.");

				int count = 0;
				var newContent = regex.Replace(normalizedContent, m => ++count == occurrence ? text : m.Value);
				newContent = PreserveLineEndings(originalContent, newContent);

				return (newContent, null);
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
					return (null, $"No lines matched pattern '{pattern}'.");

				var targetIndices = FilterOccurrences(matchedLineIndices, occurrence);
				if (targetIndices == null)
					return (null, $"Occurrence {occurrence} not found. Only {matchedLineIndices.Count} occurrence(s) exist.");

				var operationLines = string.IsNullOrEmpty(text)
					? new List<string>()
					: NormalizeLineEndings(text).Split('\n').ToList();

				// For line-based operations, match length is 1 (single line)
				var (totalInsertions, totalDeletions) = ApplyLineOperations(fileLines, targetIndices, 1, operationLines, operation);

				var newContent = string.Join("\n", fileLines);
				newContent = PreserveLineEndings(originalContent, newContent);

				if (newContent == originalContent)
					return (null, "No changes were made (content is identical).");

				return (newContent, null);
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

		private static (int totalInsertions, int totalDeletions) ApplyLineOperations(
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

			return (totalInsertions, totalDeletions);
		}

		#endregion
	}
}
