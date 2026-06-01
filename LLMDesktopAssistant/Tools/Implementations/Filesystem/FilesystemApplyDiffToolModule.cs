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
		private readonly FileAccessService _fileAccess;

		public FilesystemApplyDiffToolModule(FileAccessService fileAccess)
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
					AskForConfirmation = true
				});
		}

		public ReactiveToolResult ApplyDiff(
			string path,
			[Description("Range of lines to delete, e.g. '10-20' or '10'. If not specified, no lines will be deleted.")]
			string? deleteLines = null,
			[Description("The line number at which to insert text. Must be specified if 'insertText' is provided.")]
			int? insertAtLine = null,
			[Description("The text to insert at the specified line. Must be specified if 'insertAtLine' is provided.")]
			string? insertText = null)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);
				var fileName = Path.GetFileName(fullPath);

				if (!File.Exists(fullPath))
					return ReactiveToolResult.CreateError("File not found.");

				if (FileUtils.IsBinaryFile(fullPath))
					return ReactiveToolResult.CreateError("Cannot apply diff to binary files.");

				var originalContent = File.ReadAllText(fullPath);
				var lines = originalContent.Split(["\r\n", "\n", "\r"], StringSplitOptions.None).ToList();
				var beforeDeletionLines = lines.ToList();
				var beforeInsertionLines = beforeDeletionLines;

				int deletedStartLine = -1;
				int deletedEndLine = -1;
				List<string> deletedContent = new();

				int insertedStartLine = -1;
				int insertedEndLine = -1;
				List<string> insertedContent = new();

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

				if (insertAtLine != null && insertText != null)
				{
					if (insertAtLine < 1)
						return ReactiveToolResult.CreateError($"Line number {insertAtLine} must be at least 1");

					if (insertAtLine > lines.Count + 1)
						return ReactiveToolResult.CreateError(
							$"Line number {insertAtLine} is out of range. File has {lines.Count} lines. " +
							$"Max insert position is {lines.Count + 1}");
				}
				else if (insertAtLine != null && insertText == null)
				{
					return ReactiveToolResult.CreateError("insertText parameter is required when insertAtLine is specified");
				}
				else if (insertText != null && insertAtLine == null)
				{
					return ReactiveToolResult.CreateError("insertAtLine parameter is required when insertText is specified");
				}

				if (!string.IsNullOrEmpty(deleteLines))
				{
					var (startLine, endLine) = ParseLineRange(deleteLines);

					deletedStartLine = startLine;
					deletedEndLine = endLine;
					deletedContent = lines.Skip(startLine - 1).Take(endLine - startLine + 1).ToList();

					int startIndex = startLine - 1;
					int countToRemove = endLine - startLine + 1;
					lines.RemoveRange(startIndex, countToRemove);

					if (insertAtLine != null && insertAtLine > startLine)
					{
						if (insertAtLine < startLine + countToRemove)
						{
							insertAtLine = startLine;
						}
						else
						{
							insertAtLine -= countToRemove;
						}
					}
				}

				if (insertAtLine != null && insertText != null)
				{
					beforeInsertionLines = lines.ToList();
					insertedStartLine = insertAtLine.Value;
					var insertLinesList = insertText.Split(["\r\n", "\n", "\r"], StringSplitOptions.None).ToList();
					insertedContent = insertLinesList;
					insertedEndLine = insertAtLine.Value + insertLinesList.Count - 1;

					int insertPosition = insertAtLine.Value - 1;
					lines.InsertRange(insertPosition, insertLinesList);
				}

				var newContent = string.Join(Environment.NewLine, lines);
				if (newContent == originalContent)
				{
					var noChangeResult = new ReactiveToolResult
					{
						StatusIcon = Material.Icons.MaterialIconKind.Information,
						StatusTitle = $"**{fileName}** *({LocalizationManager.LocalizeStatic("fs-changes_none")})*",
						ResultContent = "No changes applied to the file."
					};
					return noChangeResult.Complete(true);
				}

				File.WriteAllText(fullPath, newContent);

				var changeReport = BuildChangeReport(
					beforeDeletionLines,
					beforeInsertionLines,
					lines,
					deletedStartLine, deletedEndLine, deletedContent,
					insertedStartLine, insertedEndLine, insertedContent,
					path);

				var totalChanges = (deletedContent.Count > 0 ? 1 : 0) + (insertedContent.Count > 0 ? 1 : 0);
				var changeDescription = totalChanges switch
				{
					2 => LocalizationManager.LocalizeStatic("fs-changes_modified"),
					1 when deletedContent.Count > 0 => LocalizationManager.LocalizeStatic("fs-changes_deleted"),
					1 when insertedContent.Count > 0 => LocalizationManager.LocalizeStatic("fs-changes_inserted"),
					_ => LocalizationManager.LocalizeStatic("fs-changes_updated")
				};

				string deletedLinesInfo = deletedContent.Count > 0 ? $" -{deletedContent.Count}" : string.Empty;
				string insertedLinesInfo = insertedContent.Count > 0 ? $" +{insertedContent.Count}" : string.Empty;

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.FileDocumentEdit,
					StatusTitle = $"**{fileName}** *({changeDescription}{deletedLinesInfo}{insertedLinesInfo})*",
					ResultContent = changeReport
				};

				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error applying diff: {ex.Message}");
			}
		}

		private string BuildChangeReport(
			List<string> beforeDeletionLines,
			List<string> beforeInsertionLines,
			List<string> newLines,
			int deletedStartLine, int deletedEndLine, List<string> deletedContent,
			int insertedStartLine, int insertedEndLine, List<string> insertedContent,
			string filePath)
		{
			// TODO: FIX REPORTING

			var report = new StringBuilder();

			report.AppendLine($"File modified: {filePath}");
			report.AppendLine();

			if (deletedContent.Any())
			{
				report.AppendLine($"DELETED lines {deletedStartLine}-{deletedEndLine} ({deletedContent.Count} lines)");
				report.AppendLine();

				int beforeStart = Math.Max(0, deletedStartLine - 6);
				int beforeEnd = Math.Max(0, deletedStartLine - 1);
				if (beforeStart < beforeEnd)
				{
					for (int i = beforeStart; i < beforeEnd; i++)
					{
						report.AppendLine($"{i + 1,6}: {beforeDeletionLines[i]}");
					}
					report.AppendLine();
				}

				for (int i = 0; i < Math.Min(5, deletedContent.Count); i++)
				{
					report.AppendLine($"> {deletedStartLine + i,6}: {deletedContent[i]}");
				}

				if (deletedContent.Count > 10)
				{
					report.AppendLine("> ...");
				}

				if (deletedContent.Count > 5)
				{
					int startIdx = Math.Max(Math.Min(5, deletedContent.Count), deletedContent.Count - 5);
					for (int i = startIdx; i < deletedContent.Count; i++)
					{
						report.AppendLine($"> {deletedStartLine + i,6}: {deletedContent[i]}");
					}
				}
				report.AppendLine();

				int afterStart = Math.Min(beforeInsertionLines.Count - 1, deletedStartLine - 1);
				int afterEnd = Math.Min(beforeInsertionLines.Count, deletedStartLine + 4);
				if (afterStart < afterEnd)
				{
					for (int i = afterStart; i < afterEnd; i++)
					{
						report.AppendLine($"{i + 1,6}: {beforeInsertionLines[i]}");
					}
					report.AppendLine();
				}
			}

			if (insertedContent.Any())
			{
				report.AppendLine($"INSERTED {insertedContent.Count} lines at position {insertedStartLine} (lines {insertedStartLine}-{insertedEndLine})");
				report.AppendLine();

				int beforeStart = Math.Max(0, insertedStartLine - 6);
				int beforeEnd = Math.Max(0, insertedStartLine - 1);
				if (beforeStart < beforeEnd)
				{
					for (int i = beforeStart; i < beforeEnd; i++)
					{
						if (i < beforeInsertionLines.Count)
						{
							report.AppendLine($"{i + 1,6}: {beforeInsertionLines[i]}");
						}
					}
					report.AppendLine();
				}

				for (int i = 0; i < Math.Min(5, insertedContent.Count); i++)
				{
					report.AppendLine($"> {insertedStartLine + i,6}: {insertedContent[i]}");
				}

				if (insertedContent.Count > 10)
				{
					report.AppendLine("> ...");
				}

				if (insertedContent.Count > 5)
				{
					int startIdx = Math.Max(Math.Min(5, insertedContent.Count), insertedContent.Count - 5);
					for (int i = startIdx; i < insertedContent.Count; i++)
					{
						report.AppendLine($"> {insertedStartLine + i,6}: {insertedContent[i]}");
					}
				}
				report.AppendLine();

				int afterStart = Math.Min(newLines.Count - 1, insertedEndLine);
				int afterEnd = Math.Min(newLines.Count, insertedEndLine + 5);
				if (afterStart < afterEnd)
				{
					for (int i = afterStart; i < afterEnd; i++)
					{
						report.AppendLine($"{i + 1,6}: {newLines[i]}");
					}
					report.AppendLine();
				}
			}

			return report.ToString();
		}

		private (int startLine, int endLine) ParseLineRange(string lineRange)
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
