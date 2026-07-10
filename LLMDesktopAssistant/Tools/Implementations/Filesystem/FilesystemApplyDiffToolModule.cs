using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils.Files;

namespace LLMDesktopAssistant.Tools.Implementations.Filesystem
{
	[ToolModule]
	public class FilesystemApplyDiffToolModule : ToolModule
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
					DefaultExpectedBehaviour = ToolBehaviour.FileEdit
				});
		}

		public ReactiveToolResult ApplyDiff(
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
				var fullPath = _fileAccess.AccessPath(path);

				if (!File.Exists(fullPath))
					return ReactiveToolResult.CreateError("File not found.");

				if (FileUtils.IsBinaryFile(fullPath))
					return ReactiveToolResult.CreateError("Cannot apply diff to binary files.");

				int? _insertBeforeLine = null;
				if (insertBeforeLine != null)
				{
					if (int.TryParse(insertBeforeLine, out var ibl))
						_insertBeforeLine = ibl;
					else
						return ReactiveToolResult.CreateError($"Invalid line number for insertion: {insertBeforeLine}.");
				}

				var originalContent = File.ReadAllText(fullPath);
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
						return ReactiveToolResult.CreateError($"Start line {startLine} is out of range. File has {lines.Count} lines.");

					if (endLine < startLine)
						return ReactiveToolResult.CreateError($"End line {endLine} must be greater than or equal to start line {startLine}");

					if (endLine > lines.Count)
						return ReactiveToolResult.CreateError($"End line {endLine} is out of range. File has {lines.Count} lines.");
				}

				if (insertBeforeLine != null && insertText != null)
				{
					if (_insertBeforeLine < 1)
						return ReactiveToolResult.CreateError($"Line number {insertBeforeLine} must be at least 1");

					if (_insertBeforeLine > lines.Count + 1)
						return ReactiveToolResult.CreateError(
							$"Line number {insertBeforeLine} is out of range. File has {lines.Count} lines. " +
							$"Max insert position is {lines.Count + 1}");
				}
				else if (insertBeforeLine != null && insertText == null && deleteLines == null)
				{
					return ReactiveToolResult.CreateError("either insertText or deleteLines parameters are required when insertBeforeLine is specified");
				}
				else if (insertText != null && insertBeforeLine == null)
				{
					return ReactiveToolResult.CreateError("insertBeforeLine parameter is required when insertText is specified");
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

				var newContent = string.Join(Environment.NewLine, lines);
				if (newContent == originalContent)
				{
					var noChangeResult = new ReactiveToolResult
					{
						StatusIcon = Material.Icons.MaterialIconKind.Information,
						StatusTitle = $"**{path}** *({LocalizationManager.LocalizeStatic("fs-changes_none")})*",
						ResultContent = "No changes applied to the file."
					};
					return noChangeResult.Complete(true);
				}

				File.WriteAllText(fullPath, newContent);

				var diff = UnifiedDiff.Compute(originalContent, newContent, contextLines: 10);
				var (removed, added) = diff.GetChangeCounts();

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.FileDocumentEdit,
					StatusTitle = $"**{path}** *(-{removed} +{added})*",
					ResultContent = diff.ToString()
				};

				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error applying diff: {ex.Message}");
			}
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
