using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using Material.Icons;

namespace LLMDesktopAssistant.Tools.Implementations.Filesystem
{
	/// <summary>
	/// Universal file editing tool that applies a list of replacement patches to a text file.
	/// Supports plain text (with flexible whitespace matching) and regex patterns.
	/// </summary>
	[ToolModule]
	public class FilesystemEditToolModule : FileSystemEditBaseToolModule
	{
		private readonly IWorkingDirectoryAccessService _fileAccess;

		public FilesystemEditToolModule(IWorkingDirectoryAccessService fileAccess)
		{
			_fileAccess = fileAccess;

			AddTool(new ToolInitializationInfo
			{
				Executor = Edit,
				StreamingAnalyzer = EditStreaming,
				PreviewExecutor = EditPreview,
				Name = "fs-edit",
				Description = """
					Edits a text file by applying a list of replacement patches.

					Each patch replaces ALL occurrences of its `match` with the `replace` text.
					Patches are applied sequentially: each patch sees the result of the previous ones.
					If a patch finds no occurrences, it is skipped and reported in the result —
					all other patches are still applied.

					Plain text mode (useRegex = false, default):
					- `match` is treated as a literal string (can be multi-line)
					- Leading/trailing whitespace and common indentation are ignored in matching:
					  `match: "public class Foo"` matches `"    public class Foo"` in the file
					- Text before and after the match is preserved

					Regex mode (useRegex = true):
					- `match` is interpreted as a .NET regular expression
					- `replace` supports standard replacement syntax: $1, ${name}, $&, $$, etc.
					- To delete a match, pass an empty `replace` string

					Examples:
					- Replace text (plain):
						fs-edit(path: "file.cs", patches: [{ match: "public class Foo", replace: "public class Bar" }])
					- Regex rename with capture group:
						fs-edit(path: "file.cs", patches: [{ match: "class (\\w+)", replace: "class Renamed_$1", useRegex: true }])
					- Delete all matches (empty replace):
						fs-edit(path: "file.cs", patches: [{ match: "Console\\.WriteLine", replace: "", useRegex: true }])
					- Multiple patches in one call:
						fs-edit(path: "file.cs", patches: [
							{ match: "foo", replace: "bar" },
							{ match: "class (\\w+)", replace: "class C_$1", useRegex: true }
						])
					""",
				TitleKey = Locale.GetKey("tool.name.fs-edit"),
				DescriptionKey = Locale.GetKey("tool.description.fs-edit"),
				CategoryKey = Locale.GetKey("tool.category.filesystem"),
				DefaultExpectedBehaviour = ToolBehaviour.FileEdit | ToolBehaviour.AccessOutsideWorkdir,
				DefaultSelfHandledDecisions = ToolPolicyDecision.Approve | ToolPolicyDecision.Ask,
				SynchronizationGroup = FileSystemEditBaseToolModule.SyncGroup
			});
		}

		/// <summary>
		/// A single edit operation that replaces all occurrences of a match with a replacement text.
		/// </summary>
		public sealed class EditPatch
		{
			/// <summary>
			/// Gets or sets the text or regex pattern to search for.
			/// Can include multiple lines in plain text mode.
			/// </summary>
			[Required]
			[Description("The text or regex pattern to search for. Can include multiple lines in plain text mode.")]
			public required string match { get; init; }

			/// <summary>
			/// Gets or sets the replacement text. An empty string deletes the match.
			/// In regex mode standard replacement syntax is supported: $1, ${name}, $&, $$, etc.
			/// </summary>
			[Required]
			[Description("The replacement text. An empty string deletes the match. In regex mode supports $1, ${name}, $&, $$.")]
			public string replace { get; init; } = "";

			/// <summary>
			/// Gets or sets a value indicating whether <see cref="match"/> is a .NET regular expression.
			/// </summary>
			[DefaultValue(false)]
			[Description("If true, 'match' is treated as a .NET regular expression instead of plain text.")]
			public bool useRegex { get; init; } = false;

			/// <summary>
			/// Gets or sets a value indicating whether case is ignored when matching.
			/// </summary>
			[DefaultValue(false)]
			[Description("If true, case is ignored when matching.")]
			public bool ignoreCase { get; init; } = false;
		}

		private string? CheckArgs(string path, string? fullPath, List<EditPatch> patches)
		{
			if (fullPath == null)
				return $"Access outside working directory is not allowed: {path}";

			if (!File.Exists(fullPath))
				return $"File not found: {path}";

			if (patches == null || patches.Count == 0)
				return "'patches' parameter cannot be empty.";

			foreach (var patch in patches)
			{
				if (string.IsNullOrWhiteSpace(patch.match))
					return "'patches[].match' cannot be empty.";
			}

			return null;
		}

		private class FSWriteSharedContext
		{
			public required string Path { get; init; }
			public required string NewContent { get; init; }
			public IReadOnlyList<string> PatchErrors { get; init; } = [];
		}

		private StreamingToolArgumentsAnalysisResult EditStreaming(
			string? path)
		{
			path ??= "?";
			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.FileDocumentEdit,
				StatusTitle = $"**{path}**"
			};
		}

		private PreviewToolExecutionResult EditPreview(
			[SharedContext] ref FSWriteSharedContext? sharedCtx,
			string path,
			List<EditPatch> patches,
			CancellationToken cancellationToken = default)
		{
			var fullPath = _fileAccess.CheckedAccessPath(path, DirectoryAccessMode.ReadWrite, out var isAccessed);
			var error = CheckArgs(path, fullPath, patches);
			if (error != null)
			{
				return new PreviewToolExecutionResult
				{
					InterruptingSuccess = false,
					InterruptingContent = error,
					StatusIcon = MaterialIconKind.FileAlert,
					StatusTitle = $"**{path}**",
					ExpectedBehaviour = ToolBehaviour.None |
						(!isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None)
				};
			}

			var originalContent = File.ReadAllText(fullPath!);
			var normalizedContent = NormalizeLineEndings(originalContent);
			var (newContent, patchErrors) = ApplyPatches(normalizedContent, patches, cancellationToken);
			newContent = PreserveLineEndings(originalContent, newContent);

			if (newContent == originalContent)
			{
				sharedCtx = new FSWriteSharedContext
				{
					Path = fullPath!,
					NewContent = originalContent,
					PatchErrors = patchErrors
				};

				return new PreviewToolExecutionResult
				{
					InterruptingSuccess = true,
					InterruptingContent = patchErrors.Count > 0
						? $"No changes were made to the file.\n{string.Join("\n", patchErrors)}"
						: "No changes were made to the file. The specified matches were not found.",
					StatusIcon = MaterialIconKind.FileQuestion,
					StatusTitle = LocalizationManager.LocalizeStaticFormat("tool.status.fs-edit.changes_none", $"**{path}**"),
					ExpectedBehaviour = ToolBehaviour.None |
						(!isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None)
				};
			}

			sharedCtx = new FSWriteSharedContext
			{
				Path = fullPath!,
				NewContent = newContent,
				PatchErrors = patchErrors
			};

			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.FileDocumentEdit,
				StatusTitle = $"**{path}**",
				ExpectedBehaviour = ToolBehaviour.FileEdit |
					(!isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None)
			};
		}

		private async Task Edit(
			[SharedContext] FSWriteSharedContext? sharedCtx,
			ReactiveToolResult result,
			ToolExecutionContext ctx,
			CancellationToken cancellationToken,
			[Description("The path to the file to edit.")]
			string path,
			[Description("The list of patches to apply. Each patch replaces all occurrences of its match with the replacement text.")]
			List<EditPatch> patches)
		{
			try
			{
				var fullPath = sharedCtx?.Path ?? _fileAccess.AccessPath(path, DirectoryAccessMode.ReadWrite);
				var error = CheckArgs(path, fullPath, patches);

				if (error != null)
				{
					result.StatusIcon = MaterialIconKind.FileAlert;
					result.StatusTitle = $"**{path}**";
					result.ResultContent = error;
					result.CompleteWithError();
					return;
				}

				var originalContent = File.ReadAllText(fullPath!);
				var newContent = sharedCtx?.NewContent;
				var patchErrors = sharedCtx?.PatchErrors ?? [];
				if (newContent == null)
				{
					var normalizedContent = NormalizeLineEndings(originalContent);
					(newContent, patchErrors) = ApplyPatches(normalizedContent, patches, cancellationToken);
					newContent = PreserveLineEndings(originalContent, newContent);
				}

				if (newContent == originalContent)
				{
					result.StatusIcon = MaterialIconKind.FileQuestion;
					result.StatusTitle = LocalizationManager.LocalizeStaticFormat("tool.status.fs-edit.changes_none", $"**{path}**");
					result.ResultContent = patchErrors.Count > 0
						? $"No changes were made to the file.\n{string.Join("\n", patchErrors)}"
						: "No changes were made to the file. The specified matches were not found.";
					result.CompleteWithSuccess();
					return;
				}

				var postProcessResult = await PostProcessDiffAsync(fullPath, originalContent, newContent, ctx, cancellationToken);
				string userNotesPostfix = string.IsNullOrWhiteSpace(postProcessResult.UserNotes)
					? string.Empty
					:	$"""
							
							User has provided notes:
							{postProcessResult.UserNotes}
							""";
				if (!postProcessResult.AppliedDiff.HasGroups)
				{
					result.StatusIcon = MaterialIconKind.FileDiscard;
					result.StatusTitle = $"**{path}**";
					result.ResultContent = postProcessResult.RejectedDiff.HasGroups ?
						$"""
						User has rejected the changes, none has applied.
						[REJECTED CHANGES BY THE USER, THESE ARE NOT APPLIED]:
						{postProcessResult.RejectedDiff}{userNotesPostfix}
						""" :
						$"User has rejected the changes, none has applied.{userNotesPostfix}";
					result.CompleteWithSuccess();
					return;
				}

				File.WriteAllText(fullPath!, postProcessResult.NewContent);

				var diff = postProcessResult.AppliedDiff;
				var (removed, added) = diff.GetChangeCounts();

				string notAppliedText = patchErrors.Count > 0
					? $"""
						
						[NOT APPLIED PATCHES]:
						{string.Join("\n", patchErrors)}
						"""
					: string.Empty;

				result.StatusIcon = MaterialIconKind.FileDocumentEdit;
				result.StatusTitle = $"**{path}** *(-{removed} +{added})*";
				result.ResultContent = postProcessResult.RejectedDiff.HasGroups ?
					$"""
					File edited successfully. *(-{removed} +{added})*
					[APPLIED CHANGES]:
					{diff}
					[REJECTED CHANGES BY THE USER, THESE ARE NOT APPLIED]:
					{postProcessResult.RejectedDiff}{userNotesPostfix}{notAppliedText}
					""" :
					$"""
					File edited successfully. *(-{removed} +{added})*
					[APPLIED CHANGES]:
					{diff}{userNotesPostfix}{notAppliedText}
					""";
				result.CompleteWithSuccess();
			}
			catch (OperationCanceledException)
			{
				result.StatusIcon = MaterialIconKind.FileQuestion;
				result.StatusTitle = $"**{path}**";
				result.ResultContent = "Edit operation was cancelled.";
				result.CompleteWithError();
			}
			catch (Exception ex)
			{
				result.StatusIcon = MaterialIconKind.FileAlert;
				result.StatusTitle = $"**{path}**";
				result.ResultContent = $"Error editing file: {ex.Message}";
				result.CompleteWithError();
			}
		}

		private static (string NewContent, List<string> PatchErrors) ApplyPatches(
			string content,
			List<EditPatch> patches,
			CancellationToken cancellationToken)
		{
			var patchErrors = new List<string>();
			var current = content;

			for (int i = 0; i < patches.Count; i++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var patch = patches[i];

				string? newContent;
				try
				{
					newContent = patch.useRegex
						? ApplyRegexPatch(current, patch)
						: ApplyPlainPatch(current, patch);
				}
				catch (ArgumentException ex)
				{
					patchErrors.Add($"Patch #{i + 1} (match: \"{patch.match}\"): invalid pattern: {ex.Message}");
					continue;
				}

				if (newContent == null)
					patchErrors.Add($"Patch #{i + 1} (match: \"{patch.match}\"): no occurrences found.");
				else
					current = newContent;
			}

			return (current, patchErrors);
		}

		private static string? ApplyPlainPatch(string content, EditPatch patch)
		{
			var comparison = patch.ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			var matchLines = NormalizeLineEndings(patch.match).Split('\n').ToList();

			// Remove trailing empty line if present (LLMs love to add them!)
			if (matchLines.Count > 0 && string.IsNullOrEmpty(matchLines[^1]))
				matchLines.RemoveAt(matchLines.Count - 1);

			// Apply dedent and trim each line for comparison
			matchLines = DedentLines(matchLines);
			matchLines = matchLines.Select(l => l.Trim()).ToList();

			if (matchLines.Count == 0)
				return null;

			var replacement = NormalizeLineEndings(patch.replace);
			var fileLines = content.Split('\n').ToList();
			var foundAny = false;

			if (matchLines.Count == 1)
			{
				// Single-line match: replace all non-overlapping occurrences in every line,
				// preserving the surrounding text and the file's indentation
				var match = matchLines[0];
				var replacementLines = replacement.Split('\n').ToList();
				for (int i = 0; i < fileLines.Count; i++)
				{
					var newLine = ReplaceOccurrencesInLine(fileLines[i], match, replacementLines, comparison);
					if (newLine != fileLines[i])
					{
						foundAny = true;
						fileLines[i] = newLine;
					}
				}
			}
			else
			{
				// Multi-line match: find aligned blocks of lines and replace them
				var replacementLines = replacement.Length == 0
					? []
					: replacement.Split('\n').ToList();

				for (int i = 0; i <= fileLines.Count - matchLines.Count; i++)
				{
					var prefixes = new string[matchLines.Count];
					var suffixes = new string[matchLines.Count];
					var blockMatched = true;
					for (int j = 0; j < matchLines.Count; j++)
					{
						var line = fileLines[i + j];
						var index = line.IndexOf(matchLines[j], comparison);
						if (index < 0)
						{
							blockMatched = false;
							break;
						}

						prefixes[j] = line[..index];
						suffixes[j] = line[(index + matchLines[j].Length)..];

						// The match must be aligned with the line: non-whitespace text on both
						// sides of the match would make the replacement position ambiguous
						if (HasNonWhitespace(prefixes[j]) && HasNonWhitespace(suffixes[j]))
						{
							blockMatched = false;
							break;
						}
					}

					if (!blockMatched)
						continue;

					foundAny = true;

					List<string> newBlockLines;
					if (replacementLines.Count == 0)
					{
						// Delete: keep the text before the match (first line) and after it (last line)
						newBlockLines = [prefixes[0] + suffixes[^1]];
					}
					else if (replacementLines.Count == matchLines.Count)
					{
						// Line-by-line replacement: each line inherits the file's surrounding text
						// and indentation instead of the replacement's own leading whitespace
						newBlockLines = new List<string>(matchLines.Count);
						for (int j = 0; j < matchLines.Count; j++)
							newBlockLines.Add(prefixes[j] + replacementLines[j].TrimStart() + suffixes[j]);
					}
					else
					{
						// Different line count: keep the text before the match (first line)
						// and after the match (last line)
						newBlockLines = [prefixes[0] + replacementLines[0].TrimStart()];
						newBlockLines.AddRange(replacementLines.Skip(1));
						newBlockLines[^1] += suffixes[^1];
					}

					fileLines.RemoveRange(i, matchLines.Count);
					fileLines.InsertRange(i, newBlockLines);

					// Continue scanning after the replaced block; the for-loop's i++ moves past it
					i += newBlockLines.Count - 1;
				}
			}

			return foundAny ? string.Join("\n", fileLines) : null;
		}

		private static string ReplaceOccurrencesInLine(
			string line, string match, List<string> replacementLines, StringComparison comparison)
		{
			var result = new StringBuilder();
			int pos = 0;
			while (true)
			{
				int index = line.IndexOf(match, pos, comparison);
				if (index < 0)
					break;

				result.Append(line, pos, index - pos);

				var prefix = line[pos..index];
				var firstLine = replacementLines.Count > 0 ? replacementLines[0] : "";
				// If the text before the match is pure indentation, keep the file's indentation
				// and strip the replacement's own leading whitespace to avoid doubling it
				result.Append(IsIndentation(prefix) ? firstLine.TrimStart() : firstLine);

				for (int k = 1; k < replacementLines.Count; k++)
				{
					result.Append('\n');
					result.Append(replacementLines[k]);
				}

				pos = index + match.Length;
			}

			if (pos == 0)
				return line;

			result.Append(line, pos, line.Length - pos);
			return result.ToString();
		}

		private static bool IsIndentation(string text)
			=> text.Length > 0 && text.All(c => c is ' ' or '\t');

		private static bool HasNonWhitespace(string text)
			=> text.Any(c => c is not ' ' and not '\t');

		private static string? ApplyRegexPatch(string content, EditPatch patch)
		{
			var options = RegexOptions.Compiled | (patch.ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
			var regex = new Regex(patch.match, options);

			return regex.IsMatch(content) ? regex.Replace(content, patch.replace) : null;
		}

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
	}
}
