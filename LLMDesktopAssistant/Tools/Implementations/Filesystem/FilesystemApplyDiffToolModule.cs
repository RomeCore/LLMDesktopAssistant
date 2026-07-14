using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils.Files;
using Material.Icons;
using Microsoft.Extensions.FileSystemGlobbing.Internal;

namespace LLMDesktopAssistant.Tools.Implementations.Filesystem
{
	[ToolModule]
	public class FilesystemApplyDiffToolModule : FileSystemEditBaseToolModule
	{
		private readonly WorkingDirectoryAccessService _fileAccess;

		public FilesystemApplyDiffToolModule(WorkingDirectoryAccessService fileAccess)
		{
			_fileAccess = fileAccess;

			AddTool(ApplyDiff,
				new ToolInitializationInfo
				{
					Name = "fs-apply_diff",
					Description = """
						Applies diff operations to a file.
						Supports deleting a range of lines and/or inserting text at a specific line.
						Inserting text at a specific line works next way:
						1: first line
						2: second line
						3: third line
						After inserting at 2 line:
						1: first line
						2: inserted line <- insterted here
						3: second line
						4: third line
						Note: Works best if you know line numbers when looking file with fs-explore(showLineNumbers = true)
						""",
					Category = "filesystem",
					DefaultExpectedBehaviour = ToolBehaviour.FileEdit | ToolBehaviour.AccessOutsideWorkdir,
					DefaultSelfHandledDecisions = ToolPolicyDecision.Approve | ToolPolicyDecision.Ask,
					SynchronizationGroup = FileSystemEditBaseToolModule.SyncGroup
				});
		}

		public class FSApplyDiffSharedContext
		{
			public required string Path { get; init; }
			public required string NewContent { get; init; }
		}

		public StreamingToolArgumentsAnalysisResult ApplyDiffStreaming(
			string? path)
		{
			path ??= "?";
			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.FileDocumentEdit,
				StatusTitle = $"**{path}**"
			};
		}

		public PreviewToolExecutionResult ApplyDiffPreview(
			[SharedContext] ref FSApplyDiffSharedContext? sharedCtx,
			string path, string? deleteLines = null, string? insertBeforeLine = null, string? insertText = null)
		{
			var fullPath = _fileAccess.CheckedAccessPath(path, out var isAccessed);

			if (!File.Exists(fullPath))
			{
				new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FileDocumentEdit,
					StatusTitle = $"**{path}**",
					ExpectedBehaviour = !isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = false,
					InterruptingContent = $"File or directory not found: {path}"
				};
			}

			var originalContent = File.ReadAllText(fullPath!);
			var (newContent, errorMessage) = Edit(originalContent, deleteLines, insertBeforeLine, insertText);

			if (newContent == null || newContent == originalContent)
			{
				sharedCtx = new FSApplyDiffSharedContext
				{
					Path = fullPath!,
					NewContent = originalContent
				};

				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FileQuestion,
					StatusTitle = LocalizationManager.LocalizeStaticFormat("fs-edit_changes_applied_none", $"**{path}**"),
					ExpectedBehaviour = !isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = true,
					InterruptingContent = errorMessage ?? "No changes were made to the file. The specified match was not found.",
				};
			}

			sharedCtx = new FSApplyDiffSharedContext
			{
				Path = fullPath!,
				NewContent = newContent
			};

			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.FileDocumentEdit,
				StatusTitle = $"**{path}**",
				ExpectedBehaviour = ToolBehaviour.FileEdit |
					(!isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None)
			};
		}

		public async Task ApplyDiff(
			[SharedContext] FSApplyDiffSharedContext? sharedCtx,
			ReactiveToolResult result,
			ToolExecutionContext ctx,
			CancellationToken cancellationToken,
			string path,
			[Description("Range of lines to delete, e.g. '10-20' or '10'. If not specified, no lines will be deleted.")]
			string? deleteLines = null,
			[Description("The line number at which to insert text (specified in 'insertText' or 'deleteLines').")]
			string? insertBeforeLine = null,
			[Description("The text to insert at the specified line. Requires the 'insertBeforeLine' to be specified.")]
			string? insertText = null)
		{
			try
			{
				var fullPath = sharedCtx?.Path ?? _fileAccess.AccessPath(path);

				if (!File.Exists(fullPath))
				{
					result.StatusIcon = MaterialIconKind.FileAlert;
					result.StatusTitle = $"**{path}**";
					result.ResultContent = $"File '{path}' not found.";
					result.CompleteWithError();
					return;
				}

				if (FileUtils.IsBinaryFile(fullPath))
				{
					result.StatusIcon = MaterialIconKind.FileAlert;
					result.StatusTitle = $"**{path}**";
					result.ResultContent = "Cannot apply diff to binary files.";
					result.CompleteWithError();
					return;
				}

				var originalContent = File.ReadAllText(fullPath);
				var newContent = sharedCtx?.NewContent;
				if (newContent == null)
				{
					var edited = Edit(originalContent, deleteLines, insertBeforeLine, insertText);

					if (edited.Item2 != null)
					{
						result.StatusIcon = MaterialIconKind.FileAlert;
						result.StatusTitle = $"**{path}**";
						result.ResultContent = edited.Item2;
						result.CompleteWithError();
						return;
					}

					newContent = edited.Item1!;
				}

				if (newContent == originalContent)
				{
					result.StatusIcon = MaterialIconKind.FileQuestion;
					result.StatusTitle = $"**{path}** *({LocalizationManager.LocalizeStatic("fs-changes_none")})*";
					result.ResultContent = "No changes applied to the file.";
					result.CompleteWithSuccess();
					return;
				}

				var postProcessResult = await PostProcessDiffAsync(fullPath, originalContent, newContent, ctx, cancellationToken);
				if (!postProcessResult.AppliedDiff.HasGroups)
				{
					result.StatusIcon = MaterialIconKind.FileDiscard;
					result.StatusTitle = $"**{path}**";
					result.ResultContent = postProcessResult.RejectedDiff.HasGroups ?
						$"""
						User has rejected the changes, none has applied.
						[REJECTED CHANGES BY THE USER, THESE ARE NOT APPLIED]:
						{postProcessResult.RejectedDiff}
						""" :
						"User has rejected the changes, none has applied.";
					result.CompleteWithSuccess();
					return;
				}

				File.WriteAllText(fullPath!, postProcessResult.NewContent);

				var diff = postProcessResult.AppliedDiff;
				var (removed, added) = diff.GetChangeCounts();

				result.StatusIcon = Material.Icons.MaterialIconKind.FileDocumentEdit;
				result.StatusTitle = $"**{path}** *(-{removed} +{added})*";
				result.ResultContent = postProcessResult.RejectedDiff.HasGroups ?
					$"""
					File edited successfully. *(-{removed} +{added})*
					[APPLIED CHANGES]:
					{diff}
					[REJECTED CHANGES BY THE USER, THESE ARE NOT APPLIED]:
					{postProcessResult.RejectedDiff}
					""" :
					$"""
					File edited successfully. *(-{removed} +{added})*
					[APPLIED CHANGES]:
					{diff}
					""";
				result.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				result.StatusIcon = MaterialIconKind.FileAlert;
				result.StatusTitle = $"**{path}**";
				result.ResultContent = $"Error applying diff: {ex.Message}";
				result.CompleteWithError();
			}
		}

		private static (string?, string?) Edit(string originalContent,
			string? deleteLines = null, string? insertBeforeLine = null, string? insertText = null)
		{
			int? _insertBeforeLine = null;
			if (insertBeforeLine != null)
			{
				if (int.TryParse(insertBeforeLine, out var ibl))
					_insertBeforeLine = ibl;
				else
					return (null, $"Invalid line number for insertion: {insertBeforeLine}.");
			}

			var lines = originalContent.Split(["\r\n", "\n", "\r"], StringSplitOptions.None).ToList();
			var beforeDeletionLines = lines.ToList();
			var beforeInsertionLines = beforeDeletionLines;

			int deletedStartLine = -1;
			int deletedEndLine = -1;
			List<string> deletedContent = [];

			int insertedStartLine = -1;
			int insertedEndLine = -1;
			List<string> insertedContent = [];

			if (!string.IsNullOrEmpty(deleteLines))
			{
				var (startLine, endLine) = ParseLineRange(deleteLines);

				if (startLine < 1 || startLine > lines.Count)
					return (null, $"Start line {startLine} is out of range. File has {lines.Count} lines.");

				if (endLine < startLine)
					return (null, $"End line {endLine} must be greater than or equal to start line {startLine}");

				if (endLine > lines.Count)
					return (null, $"End line {endLine} is out of range. File has {lines.Count} lines.");
			}

			if (insertBeforeLine != null && insertText != null)
			{
				if (_insertBeforeLine < 1)
					return (null, $"Line number {insertBeforeLine} must be at least 1");

				if (_insertBeforeLine > lines.Count + 1)
					return (null,
						$"Line number {insertBeforeLine} is out of range. File has {lines.Count} lines. " +
						$"Max insert position is {lines.Count + 1}");
			}
			else if (insertBeforeLine != null && insertText == null && deleteLines == null)
			{
				return (null, "either insertText or deleteLines parameters are required when insertBeforeLine is specified");
			}
			else if (insertText != null && insertBeforeLine == null)
			{
				return (null, "insertBeforeLine parameter is required when insertText is specified");
			}

			if (!string.IsNullOrEmpty(deleteLines))
			{
				var (startLine, endLine) = ParseLineRange(deleteLines);

				deletedStartLine = startLine;
				deletedEndLine = endLine;
				deletedContent = lines.Skip(startLine - 1).Take(endLine - startLine + 1).ToList();
				insertText ??= string.Join(Environment.NewLine, deletedContent);

				int startIndex = startLine - 1;
				int countToRemove = endLine - startLine + 1;
				lines.RemoveRange(startIndex, countToRemove);

				if (_insertBeforeLine != null && _insertBeforeLine > startLine)
				{
					if (_insertBeforeLine < startLine + countToRemove)
					{
						_insertBeforeLine = startLine;
					}
					else
					{
						_insertBeforeLine -= countToRemove;
					}
				}
			}

			if (_insertBeforeLine != null && insertText != null)
			{
				beforeInsertionLines = lines.ToList();
				insertedStartLine = _insertBeforeLine.Value;
				var insertLinesList = insertText.Split(["\r\n", "\n", "\r"], StringSplitOptions.None).ToList();
				insertedContent = insertLinesList;
				insertedEndLine = _insertBeforeLine.Value + insertLinesList.Count - 1;

				int insertPosition = _insertBeforeLine.Value - 1;
				lines.InsertRange(insertPosition, insertLinesList);
			}

			return (string.Join(Environment.NewLine, lines), null);
		}

		private static (int startLine, int endLine) ParseLineRange(string lineRange)
		{
			if (string.IsNullOrEmpty(lineRange))
				throw new ArgumentException("Line range cannot be empty");

			var parts = lineRange.Split('-');

			if (parts.Length != 1 && parts.Length != 2)
				throw new ArgumentException($"Invalid line range format: {lineRange}. Expected format: 'exactLine' (e.g., '10') or 'start-end' (e.g., '15-45')");

			if (!int.TryParse(parts[0], out int startLine))
				throw new ArgumentException($"Invalid start line number: {parts[0]}");

			int endLine;
			if (parts.Length == 2)
			{
				if (!int.TryParse(parts[1], out endLine))
					throw new ArgumentException($"Invalid end line number: {parts[1]}");
			}
			else
			{
				endLine = startLine;
			}

			return (startLine, endLine);
		}
	}
}
