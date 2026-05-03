using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils.Files;
using RCLargeLanguageModels.Tools;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;
using System.Text;
using System.Text.RegularExpressions;

namespace LLMDesktopAssistant.Tools.Implementations
{
	[ToolModule]
	public class FilesystemToolModule : ToolModule
	{
		private readonly FileAccessService _fileAccess;
		private readonly IDocumentReadingService _documentReader;

		public FilesystemToolModule(FileAccessService fileAccess, IDocumentReadingService documentReader)
		{
			_fileAccess = fileAccess;
			_documentReader = documentReader;

			AddTool(GetFileInfo,
				new ToolInitializationInfo
				{
					Name = "fs-get_file_info",
					Description = "Returns file information including type classification.",
					Category = "filesystem",
					AskForConfirmation = false
				});

			AddTool(ReadFile,
				new ToolInitializationInfo
				{
					Name = "fs-read_file",
					Description = "Reads text file content from the working directory.",
					Category = "filesystem",
					AskForConfirmation = false
				});

			AddTool(ReadBinaryFile,
				new ToolInitializationInfo
				{
					Name = "fs-read_binary_file",
					Description = "Reads binary file content as hex dump from the working directory.",
					Category = "filesystem",
					AskForConfirmation = false
				});

			AddTool(ReadDocumentFile,
				new ToolInitializationInfo
				{
					Name = "fs-read_document_file",
					Description = "Reads document file content by pages from the working directory. Supported extensions: .pdf, .docx, .pptx.",
					Category = "filesystem",
					AskForConfirmation = false
				});

			AddTool(WriteFile,
				new ToolInitializationInfo
				{
					Name = "fs-write_file",
					Description = "Writes text content to a file inside working directory.",
					Category = "filesystem",
					AskForConfirmation = true
				});

			AddTool(WriteBinaryFile,
				new ToolInitializationInfo
				{
					Name = "fs-write_binary_file",
					Description = "Writes binary content to a file inside working directory.",
					Category = "filesystem",
					AskForConfirmation = true
				});

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
						Note: Works best if you know line numbers when looking file with fs-read_file(showLineNumbers = true)
						""",
					Category = "filesystem",
					AskForConfirmation = true
				});

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

			AddTool(ListDirectory,
				new ToolInitializationInfo
				{
					Name = "fs-list_directory",
					Description = "Lists files and directories inside working directory path.",
					Category = "filesystem",
					AskForConfirmation = false
				});

			AddTool(CreateDirectory,
				new ToolInitializationInfo
				{
					Name = "fs-create_directory",
					Description = "Creates a new directory inside working directory path.",
					Category = "filesystem",
					AskForConfirmation = true
				});

			AddTool(DeleteFile,
				new ToolInitializationInfo
				{
					Name = "fs-delete_file",
					Description = "Deletes a file inside working directory.",
					Category = "filesystem",
					AskForConfirmation = true
				});

			AddTool(DeleteDirectory,
				new ToolInitializationInfo
				{
					Name = "fs-delete_directory",
					Description = "Deletes a directory (empty or with contents) from the working directory.",
					Category = "filesystem",
					AskForConfirmation = true
				});

			AddTool(CopyFile,
				new ToolInitializationInfo
				{
					Name = "fs-copy_file",
					Description = "Copies a file within the working directory.",
					Category = "filesystem",
					AskForConfirmation = true
				});

			AddTool(CopyDirectory,
				new ToolInitializationInfo
				{
					Name = "fs-copy_directory",
					Description = "Copies a directory and all its contents to a new location within the working directory.",
					Category = "filesystem",
					AskForConfirmation = true
				});

			AddTool(RenameFile,
				new ToolInitializationInfo
				{
					Name = "fs-rename_file",
					Description = "Renames or moves a file within the working directory.",
					Category = "filesystem",
					AskForConfirmation = true
				});

			AddTool(MoveDirectory,
				new ToolInitializationInfo
				{
					Name = "fs-move_directory",
					Description = "Moves a directory and all its contents to a new location within the working directory.",
					Category = "filesystem",
					AskForConfirmation = true
				});

			AddTool(Grep,
				new ToolInitializationInfo
				{
					Name = "fs-grep",
					Description = """
						Searches for pattern in files using regex.
						Use this tool with care, as it can be slow and resource-intensive.
						Use
						""",
					Category = "filesystem",
					AskForConfirmation = false
				});
		}

		public ReactiveToolResult GetFileInfo(string path)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);
				var metrics = FileUtils.GetFileMetrics(fullPath);
				var fileName = Path.GetFileName(fullPath);

				var result = new ReactiveToolResult
				{
					StatusIcon = metrics.Type switch
					{
						FileType.Binary => Material.Icons.MaterialIconKind.File,
						FileType.Text => Material.Icons.MaterialIconKind.FileText,
						FileType.Code => Material.Icons.MaterialIconKind.FileCode,
						FileType.Image => Material.Icons.MaterialIconKind.FileImage,
						FileType.Audio => Material.Icons.MaterialIconKind.FileMusic,
						FileType.Video => Material.Icons.MaterialIconKind.FileVideo,
						FileType.Executable => Material.Icons.MaterialIconKind.Application,
						FileType.Archive => Material.Icons.MaterialIconKind.Archive,
						FileType.Document => Material.Icons.MaterialIconKind.FileDocument,
						_ => Material.Icons.MaterialIconKind.FileQuestion
					},
					StatusTitle = $"**{fileName}** *({FileUtils.BytesToDisplaySize(metrics.Size)})*"
				};

				var additional = metrics.LineCount != null
					? $"Lines: {metrics.LineCount}"
					: "Binary";

				var output = $"""
					Name: {metrics.Name}
					Type: {metrics.Type}
					Size: {metrics.Size} bytes ~ ({FileUtils.BytesToDisplaySize(metrics.Size)})
					{additional}
					Created: {metrics.Created:yyyy-MM-dd HH:mm}
					Modified: {metrics.Modified:yyyy-MM-dd HH:mm}
					Attributes: {metrics.Attributes}
					""";

				result.ResultContent = output;
				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error getting file info: {ex.Message}");
			}
		}

		public ReactiveToolResult ReadFile(
			string path,
			[Description("The 1-based index of the first line to read.")]
			int startLine,
			[Description("The 1-based index of the last line to read.")]
			int endLine,
			[Description("The maximum length of each line to read.")]
			int maxLineLength = 2000,
			[Description("Whether to include line numbers before every line in format '   1: *line content*'.")]
			bool showLineNumbers = false)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);
				var fileName = Path.GetFileName(fullPath);

				if (!File.Exists(fullPath))
					return ReactiveToolResult.CreateError($"File '{path}' not found.");

				if (endLine < startLine)
					return ReactiveToolResult.CreateError($"Invalid line range (startLine: {startLine}, endLine: {endLine}).");

				var (lines, totalLines) = FileUtils.ReadLinesChunk(
					fullPath,
					startLine,
					endLine - startLine + 1,
					maxLineLength,
					showLineNumbers);
				var endShown = startLine + lines.Count - 1;

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.FileCode,
					StatusTitle = endShown == totalLines ?
						(startLine == 1 ? $"**{fileName}**" : $"**{fileName}** *({startLine}~{endShown})*") :
						$"**{fileName}** *({startLine}~{endShown} / {totalLines})*"
				};

				if (lines.Count == 0)
				{
					result.ResultContent = "No content.";
					return result.Complete(true);
				}

				string nextReadTip = string.Empty;
				if (endShown < totalLines)
					nextReadTip = $" (Continue reading from line {endShown + 1}, e.g. {endShown + 1}-{Math.Min(endShown + 100, totalLines)})";

				var output = $"""
					File: {path}
					Showing: {startLine}-{endShown}{nextReadTip}
					Total lines: {totalLines}
					Contents:
					{string.Join(Environment.NewLine, lines)}
					""";

				result.ResultContent = output;
				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error reading file: {ex.Message}");
			}
		}

		public ReactiveToolResult ReadBinaryFile(
			string path,
			[Description("The 1-based index of the first byte to read.")]
			int startByte = 1,
			int endByte = 4096)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);
				var fileName = Path.GetFileName(fullPath);

				if (!File.Exists(fullPath))
					return ReactiveToolResult.CreateError("File not found.");

				if (endByte < startByte)
					return ReactiveToolResult.CreateError("Invalid byte range.");

				var fileInfo = new FileInfo(fullPath);
				var totalBytes = fileInfo.Length;

				var (lines, read) = FileUtils.ReadHexChunk(
					fullPath,
					startByte,
					endByte - startByte + 1);
				var endShown = startByte + read - 1;

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.FileCode,
					StatusTitle = endShown == totalBytes ?
						(startByte == 1 ? $"**{fileName}**" : $"**{fileName}** *({startByte}~{endShown})*") :
						$"**{fileName}** *({startByte}~{endShown} / {totalBytes})*"
				};

				if (read == 0)
				{
					result.ResultContent = "No content.";
					return result.Complete(true);
				}

				var output = $"""
					File: {path}
					Size: {totalBytes} bytes ~ ({FileUtils.BytesToDisplaySize(totalBytes)})
					Showing bytes: {startByte}-{startByte + read - 1}
					Hex dump:
					{string.Join(Environment.NewLine, lines)}
					""";

				result.ResultContent = output;
				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error reading binary file: {ex.Message}");
			}
		}

		public ReactiveToolResult ReadDocumentFile(
			string path,
			[Description("The 1-based index of the first page to read.")]
			int startPage = 1,
			[Description("The 1-based index of the last page to read.")]
			int endPage = 30)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);
				var fileName = Path.GetFileName(fullPath);

				if (!File.Exists(fullPath))
					return ReactiveToolResult.CreateError("File not found.");

				if (endPage < startPage)
					return ReactiveToolResult.CreateError("Invalid page range.");

				var text = _documentReader.ExtractText(
					fullPath,
					startPage,
					endPage - startPage + 1);

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.FileDocument,
					StatusTitle = $"**{fileName}** *({startPage}~{endPage})*"
				};

				if (string.IsNullOrWhiteSpace(text))
				{
					result.ResultContent = "No text content extracted.";
					return result.Complete(true);
				}

				var output = $"""
					File: {path}
					Pages: {startPage}-{endPage}
					Content:
					{text}
					""";

				result.ResultContent = output;
				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error reading document file: {ex.Message}");
			}
		}

		public ReactiveToolResult WriteFile(
			string path,
			string content,
			bool append = false)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);
				var fileName = Path.GetFileName(fullPath);
				var dir = Path.GetDirectoryName(fullPath);

				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir!);

				var fileExisted = File.Exists(fullPath);

				if (append)
					File.AppendAllText(fullPath, content);
				else
					File.WriteAllText(fullPath, content);

				var fileInfo = new FileInfo(fullPath);
				var size = FileUtils.BytesToDisplaySize(fileInfo.Length);

				var result = new ReactiveToolResult
				{
					StatusIcon = fileExisted ?
						(append ? Material.Icons.MaterialIconKind.FileEdit : Material.Icons.MaterialIconKind.FileCheck) :
						Material.Icons.MaterialIconKind.FilePlus,
					StatusTitle = $"**{fileName}**"
				};

				var output = $"""
					File: {path}
					Operation: {(append ? "Append" : "Write")}
					New size: {fileInfo.Length} bytes ~ ({size})
					""";

				result.ResultContent = output;
				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error writing file: {ex.Message}");
			}
		}

		public ReactiveToolResult WriteBinaryFile(
			string path,
			[Description("Hexadecimal string representing binary data in format '01 F8 2A'")]
			string hex,
			bool append = false)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);
				var fileName = Path.GetFileName(fullPath);
				var dir = Path.GetDirectoryName(fullPath);

				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir!);

				var hexClean = Regex.Replace(hex, @"[^0-9A-Fa-f]+", "");

				if (hexClean.Length % 2 != 0)
					return ReactiveToolResult.CreateError(
						"Invalid hex string length. Hex string must have an even number of characters.");

				var bytes = new byte[hexClean.Length / 2];
				for (int i = 0; i < bytes.Length; i++)
				{
					var hexByte = hexClean.Substring(i * 2, 2);
					if (!byte.TryParse(hexByte, System.Globalization.NumberStyles.HexNumber, null, out byte parsedByte))
						return ReactiveToolResult.CreateError(
							$"Invalid hex byte at position {i * 2}: '{hexByte}'");
					bytes[i] = parsedByte;
				}

				var fileExisted = File.Exists(fullPath);

				if (append)
				{
					using var fs = File.Open(fullPath, FileMode.Append);
					fs.Write(bytes, 0, bytes.Length);
				}
				else
				{
					File.WriteAllBytes(fullPath, bytes);
				}

				var fileInfo = new FileInfo(fullPath);
				var size = FileUtils.BytesToDisplaySize(fileInfo.Length);

				var result = new ReactiveToolResult
				{
					StatusIcon = fileExisted ?
						(append ? Material.Icons.MaterialIconKind.FileEdit : Material.Icons.MaterialIconKind.FileCheck) :
						Material.Icons.MaterialIconKind.FilePlus,
					StatusTitle = $"**{fileName}**"
				};

				var output = $"""
					File: {path}
					Operation: {(append ? "Append" : "Write")}
					Bytes written: {bytes.Length}
					Total size: {fileInfo.Length} bytes ~ ({size})
					""";

				result.ResultContent = output;
				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error writing binary file: {ex.Message}");
			}
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

		public ReactiveToolResult ReplaceInFile(
			string path,
			[Description("The string to search for. If null or empty, the 'oldRegex' must be provided.")]
			string? oldString = null,
			[Description("The regex to search for. If null or empty, the 'oldString' must be provided.")]
			string? oldRegex = null,
			[Description("The replacement string. If null, the oldString or oldRegex will be removed (replaced with empty string).")]
			string? newString = null)
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

				var content = File.ReadAllText(fullPath);

				var regex = oldString != null ?
					new Regex(Regex.Escape(oldString), RegexOptions.Compiled) :
					new Regex(oldRegex!, RegexOptions.Compiled);
				newString ??= string.Empty;

				var matches = regex.Matches(content);
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

				var newContent = regex.Replace(content, newString);
				File.WriteAllText(fullPath, newContent);

				var report = new StringBuilder();
				var oldPattern = oldString ?? oldRegex!;
				report.AppendLine($"Successfully replaced '{oldPattern}' with '{newString ?? "(empty)"}' in file '{path}'");
				report.AppendLine($"Summary:");
				report.AppendLine($"  Total replacements: {totalReplacements}");
				report.AppendLine($"  File size before: {content.Length} bytes");
				report.AppendLine($"  File size after: {newContent.Length} bytes");

				if (totalReplacements <= 10) // Show all replacements if 10 or less
				{
					report.AppendLine();
					report.AppendLine($"Replacement details:");
					for (int i = 0; i < matches.Count; i++)
					{
						var match = matches[i];
						var contextStart = Math.Max(0, match.Index - 20);
						var contextEnd = Math.Min(content.Length, match.Index + match.Length + 20);
						var context = content.Substring(contextStart, contextEnd - contextStart);

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
						var contextEnd = Math.Min(content.Length, match.Index + match.Length + 20);
						var context = content.Substring(contextStart, contextEnd - contextStart);

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
							var contextEnd = Math.Min(content.Length, match.Index + match.Length + 20);
							var context = content.Substring(contextStart, contextEnd - contextStart);

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

		private string Truncate(string text, int maxLength)
		{
			if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
				return text;

			return text.Substring(0, maxLength - 3) + "...";
		}

		public ReactiveToolResult ListDirectory(
			string path = "",
			[Description("The maximum depth of directories to include in the listing. 1 = current directory only.")]
			int maxDepth = 2,
			[Description("The number of entries to skip before starting the list.")]
			int offset = 0,
			[Description("The maximum number of entries to return.")]
			int maxEntries = 200,
			[Description("The list of directories to ignore. Each directory can be a pattern (e.g. '.*' to ignore all directories that starts with dot)")]
			[DefaultValue(new string[]{ ".git" })]
			string[]? ignoreDirectories = null)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);
				var dirName = path + "/";

				if (!Directory.Exists(fullPath))
					return ReactiveToolResult.CreateError("Directory not found.");

				var sb = new StringBuilder();
				int totalCount = 0;
				ignoreDirectories ??= [".git"];

				void Traverse(string currentPath, int depth, string indent)
				{
					if (depth > maxDepth)
						return;

					var entries = Directory.GetFileSystemEntries(currentPath)
						.OrderBy(e => Directory.Exists(e) ? 0 : 1)
						.ThenBy(e => e);

					foreach (var entry in entries)
					{
						var name = Path.GetFileName(entry);
						var isDir = Directory.Exists(entry);

						if (isDir)
						{
							int count = 0;
							try
							{
								count = Directory.GetFileSystemEntries(entry).Length;
							}
							catch { /* ignore */ }

							if (offset <= totalCount && totalCount <= offset + maxEntries)
							{
								var edited = Directory.GetLastWriteTime(entry).ToString("yyyy-MM-dd HH:mm");
								sb.AppendLine($"{indent}[DIR]  {name} ({count} items, {edited})");
							}

							totalCount++;

							if (ignoreDirectories.Any(igdir => FileSystemName.MatchesSimpleExpression(igdir, name)))
								sb.AppendLine($"{indent}  Directory contents not shown");
							else
								Traverse(entry, depth + 1, indent + "  ");
						}
						else
						{
							if (offset <= totalCount && totalCount <= offset + maxEntries)
							{
								var fileMetrics = FileUtils.GetFileMetrics(entry);
								var sizeStr = FileUtils.BytesToDisplaySize(fileMetrics.Size);
								var linesStr = fileMetrics.LineCount != null ? $"{fileMetrics.LineCount} lines" : "binary";
								var edited = fileMetrics.Modified.ToString("yyyy-MM-dd HH:mm");

								sb.AppendLine($"{indent}[FILE] {name} ({sizeStr}, {linesStr}, {edited})");
							}

							totalCount++;
						}
					}
				}

				sb.AppendLine($"[DIRECTORY LISTING]");
				sb.AppendLine($"Path: {path}");
				sb.AppendLine($"Max depth: {maxDepth}");
				sb.AppendLine($"Offset: {offset}, Max entries: {maxEntries}");
				if (ignoreDirectories.Length > 0)
					sb.AppendLine($"Ignored directories: {string.Join(", ", ignoreDirectories)}");
				else
					sb.AppendLine("No ignored directories.");
				sb.AppendLine();

				Traverse(fullPath, 1, "");

				if (offset > totalCount)
				{
					sb.AppendLine($"Offset is too large. (offset:{offset} > total count:{totalCount})");
				}
				else if (offset + maxEntries < totalCount)
				{
					sb.AppendLine();
					sb.AppendLine($"... truncated ({totalCount - offset - maxEntries} entries more avalable)");
				}

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.Folder,
					StatusTitle = $"**{dirName}** *({string.Format(LocalizationManager.LocalizeStatic("fs-entries"), totalCount)})*",
					ResultContent = sb.ToString()
				};

				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error listing directory: {ex.Message}");
			}
		}

		public ReactiveToolResult CreateDirectory(string path)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);
				var dirName = path + "/";

				if (Directory.Exists(fullPath))
					return ReactiveToolResult.CreateError("Directory already exists.");

				Directory.CreateDirectory(fullPath);

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.FolderPlus,
					StatusTitle = $"**{dirName}**",
					ResultContent = $"Directory '{path}' created successfully."
				};

				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error creating directory: {ex.Message}");
			}
		}

		public ReactiveToolResult DeleteFile(string path)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);
				var fileName = Path.GetFileName(fullPath);

				if (!File.Exists(fullPath))
					return ReactiveToolResult.CreateError("File not found.");

				var fileInfo = new FileInfo(fullPath);
				var size = FileUtils.BytesToDisplaySize(fileInfo.Length);

				File.Delete(fullPath);

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.Delete,
					StatusTitle = $"**{fileName}**",
					ResultContent = $"File '{path}' deleted successfully."
				};

				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error deleting file: {ex.Message}");
			}
		}

		public ReactiveToolResult DeleteDirectory(string path)
		{
			try
			{
				// Prevent deleting the root working directory
				if (path == "." || path == "" || path == "/")
					return ReactiveToolResult.CreateError("Cannot delete the root working directory.");

				var fullPath = _fileAccess.AccessPath(path);
				var dirName = path + "/";

				if (!Directory.Exists(fullPath))
					return ReactiveToolResult.CreateError("Directory not found.");

				var dirInfo = new DirectoryInfo(fullPath);

				Directory.Delete(fullPath, recursive: true);

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.FolderRemove,
					StatusTitle = $"**{dirName}**",
					ResultContent = $"Directory '{path}' deleted successfully."
				};

				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error deleting directory: {ex.Message}");
			}
		}

		public ReactiveToolResult RenameFile(
			string oldPath,
			string newPath,
			bool overwrite = false)
		{
			try
			{
				var fullOldPath = _fileAccess.AccessPath(oldPath);
				var fullNewPath = _fileAccess.AccessPath(newPath);

				var oldFileName = Path.GetFileName(fullOldPath);
				var newFileName = Path.GetFileName(fullNewPath);

				if (!File.Exists(fullOldPath))
					return ReactiveToolResult.CreateError("Source file not found.");

				if (File.Exists(fullNewPath) && !overwrite)
					return ReactiveToolResult.CreateError("Destination file already exists.");

				if (Path.GetDirectoryName(fullNewPath) is string dir)
					Directory.CreateDirectory(dir);

				File.Move(fullOldPath, fullNewPath, overwrite);

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.Pencil,
					StatusTitle = $"**{oldFileName}** → **{newFileName}**",
					ResultContent = $"File renamed from '{oldPath}' to '{newPath}'."
				};

				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error renaming file: {ex.Message}");
			}
		}

		public ReactiveToolResult MoveDirectory(
			string sourcePath,
			string destinationPath,
			bool overwrite = false)
		{
			try
			{
				var fullSourcePath = _fileAccess.AccessPath(sourcePath);
				var fullDestPath = _fileAccess.AccessPath(destinationPath);
				var dirName = Path.GetFileName(fullSourcePath);

				if (!Directory.Exists(fullSourcePath))
					return ReactiveToolResult.CreateError("Source directory not found.");

				if (Directory.Exists(fullDestPath) && !overwrite)
					return ReactiveToolResult.CreateError("Destination directory already exists.");

				Directory.CreateDirectory(Path.GetDirectoryName(fullDestPath)!);
				Directory.Move(fullSourcePath, fullDestPath);

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.Folder,
					StatusTitle = $"**{dirName}**",
					ResultContent = $"Directory moved from '{sourcePath}' to '{destinationPath}'."
				};

				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error moving directory: {ex.Message}");
			}
		}

		public ReactiveToolResult CopyFile(
			string oldPath,
			string newPath,
			bool overwrite = false)
		{
			try
			{
				var fullOldPath = _fileAccess.AccessPath(oldPath);
				var fullNewPath = _fileAccess.AccessPath(newPath);

				var oldFileName = Path.GetFileName(fullOldPath);
				var newFileName = Path.GetFileName(fullNewPath);

				if (!File.Exists(fullOldPath))
					return ReactiveToolResult.CreateError("Source file not found.");

				if (File.Exists(fullNewPath) && !overwrite)
					return ReactiveToolResult.CreateError("Destination file already exists.");

				if (Path.GetDirectoryName(fullNewPath) is string dir)
					Directory.CreateDirectory(dir);

				File.Copy(fullOldPath, fullNewPath, overwrite);

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.ContentCopy,
					StatusTitle = $"**{oldFileName}** → **{newFileName}**",
					ResultContent = $"File copied from '{oldPath}' to '{newPath}'."
				};

				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error copying file: {ex.Message}");
			}
		}

		public ReactiveToolResult CopyDirectory(
			string sourcePath,
			string destinationPath,
			bool overwrite = false)
		{
			try
			{
				var fullSourcePath = _fileAccess.AccessPath(sourcePath);
				var fullDestPath = _fileAccess.AccessPath(destinationPath);
				var dirName = Path.GetFileName(fullSourcePath);

				if (!Directory.Exists(fullSourcePath))
					return ReactiveToolResult.CreateError("Source directory not found.");

				if (Directory.Exists(fullDestPath) && !overwrite)
					return ReactiveToolResult.CreateError("Destination directory already exists.");

				Directory.CreateDirectory(fullDestPath);

				foreach (var file in Directory.GetFiles(fullSourcePath, "*", SearchOption.AllDirectories))
				{
					var relativePath = Path.GetRelativePath(fullSourcePath, file);
					var destFile = Path.Combine(fullDestPath, relativePath);
					Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
					File.Copy(file, destFile, overwrite);
				}

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.ContentCopy,
					StatusTitle = $"**{dirName}**",
					ResultContent = $"Directory copied from '{sourcePath}' to '{destinationPath}'."
				};

				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error copying directory: {ex.Message}");
			}
		}

		public ReactiveToolResult Grep(
			[Description("The regex pattern to search for.")]
			string pattern,
			[Description("The path where to search, can be file or directory path.")]
			string path,
			[Description("The file extensions to allow in search. Examples: '.cs', '.txt' etc.")]
			string[]? allowedExtensions,
			[Description("The maximum count of matched files to return.")]
			int limitFiles = 20,
			[Description("The maximum count of matched lines per file to return.")]
			int limitLinesPerFile = 20,
			[Description("Whether to ignore case when searching for the pattern.")]
			bool ignoreCase = false,
			[Description("Whether to return line numbers with the matched lines.")]
			bool lineNumbers = true,
			[Description("Whether to return non-matched lines instead of matched lines.")]
			bool invert = false,
			[Description("Whether to return only regex matches instead of entire lines.")]
			bool onlyMatching = false,
			[Description("The number of lines to show before each match.")]
			int beforeContext = 0,
			[Description("The number of lines to show after each match.")]
			int afterContext = 0,
			[Description("Whether to search recursively through subdirectories.")]
			bool recursive = true,
			CancellationToken cancellationToken = default)
		{
			try
			{
				var workingDirectory = _fileAccess.GetWorkingDirectory();
				var fullPath = _fileAccess.AccessPath(path);
				var regexIgnoreCaseOptions = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
				var regex = new Regex(pattern, regexIgnoreCaseOptions | RegexOptions.Compiled);

				var filesToSearch = new List<string>();
				var results = new List<string>();
				int totalFilesMatched = 0;

				var result = new ReactiveToolResult();

				bool fileExists = File.Exists(fullPath);
				bool dirExists = Directory.Exists(fullPath);

				if (!fileExists && !dirExists)
					return ReactiveToolResult.CreateError("File or directory not found.");

				Task.Run(() =>
				{
					try
					{
						result.StatusIcon = Material.Icons.MaterialIconKind.FileMultiple;
						result.StatusTitle = LocalizationManager.LocalizeStatic("fs-grep_collecting_files");

						if (fileExists)
						{
							filesToSearch.Add(fullPath);
						}
						else if (dirExists)
						{
							var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
							var allFiles = Directory.GetFiles(fullPath, "*", searchOption);

							foreach (var file in allFiles)
							{
								if (allowedExtensions != null && allowedExtensions.Length > 0)
								{
									var ext = Path.GetExtension(file);
									if (!allowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
										continue;
								}

								var name = Path.GetFileName(file);
								if (regex.IsMatch(name))
								{
									var relativePath = Path.GetRelativePath(workingDirectory, file);
									results.Add($"[FILE] {relativePath}");
								}

								try
								{
									// Skip binary files automatically
									if (!FileUtils.IsBinaryFile(file))
									{
										filesToSearch.Add(file);
										result.StatusTitle = string.Format(
											LocalizationManager.LocalizeStatic("fs-grep_collecting_files_count"),
											filesToSearch.Count);
									}
								}
								catch (Exception ex)
								{
									results.Add($"Error checking file {file}: {ex.Message}");
								}
							}
						}

						result.StatusIcon = Material.Icons.MaterialIconKind.FileSearch;
						result.StatusTitle = LocalizationManager.LocalizeStatic("fs-grep_scanning_files");
						result.Progress = 0;
						result.MaxProgress = filesToSearch.Count;

						foreach (var file in filesToSearch)
						{
							result.Progress++;
							cancellationToken.ThrowIfCancellationRequested();

							var relativePath = Path.GetRelativePath(workingDirectory, file);
							var fileMatches = SearchInFile(workingDirectory, file, regex, limitLinesPerFile, invert, onlyMatching,
															lineNumbers, beforeContext, afterContext, cancellationToken);
							if (fileMatches.Count > 0)
							{
								result.ResultContentLines.Add($"\n--- {relativePath} ---");
								result.ResultContentLines.AddRange(fileMatches);

								totalFilesMatched++;
								if (totalFilesMatched >= limitFiles)
									break;
							}
						}
						if (result.ResultContentLines.Count == 0)
							result.ResultContentLines.Add("No matches found.");

						result.StatusIcon = Material.Icons.MaterialIconKind.FileCheck;
						result.StatusTitle = string.Format(LocalizationManager.LocalizeStatic("fs-grep_completed"), totalFilesMatched);
						result.Complete(true);
					}
					catch
					{
						result.StatusIcon = null;
						result.StatusTitle = null;
						result.Complete(false);
					}
				}, cancellationToken);

				return result;
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Grep error: {ex.Message}");
			}
		}

		private List<string> SearchInFile(
			string workDir,
			string filePath,
			Regex regex,
			int limitLinesPerFile,
			bool invert,
			bool onlyMatching,
			bool showLineNumbers,
			int beforeContext,
			int afterContext,
			CancellationToken cancellationToken = default)
		{
			string[] lines = File.ReadAllLines(filePath);
			try
			{
				lines = File.ReadAllLines(filePath);
			}
			catch
			{
				var relativePath = Path.GetRelativePath(workDir, filePath);
				return [$"Cannot read file {relativePath}: it may be corrupted or taken by another process."];
			}

			var matches = new List<string>();
			int count = 0;

			for (int i = 0; i < lines.Length; i++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var line = lines[i];
				var isMatch = regex.IsMatch(line);

				if (invert)
					isMatch = !isMatch;

				if (!isMatch)
					continue;

				// Add context lines before
				if (beforeContext > 0)
				{
					int contextStart = Math.Max(0, i - beforeContext);
					for (int ctx = contextStart; ctx < i; ctx++)
					{
						if (!matches.Contains($"{ctx + 1}: {lines[ctx]}"))
						{
							if (showLineNumbers)
								matches.Add($"{ctx + 1}: {lines[ctx]}");
							else
								matches.Add(lines[ctx]);
						}
					}
				}

				// Add the matching line
				if (onlyMatching)
				{
					var matchesCollection = regex.Matches(line);
					foreach (Match m in matchesCollection)
					{
						if (showLineNumbers)
							matches.Add($"{i + 1}: {m.Value}");
						else
							matches.Add(m.Value);
					}
				}
				else
				{
					if (showLineNumbers)
						matches.Add($"{i + 1}: {line}");
					else
						matches.Add(line);
				}

				// Add context lines after
				if (afterContext > 0)
				{
					int contextEnd = Math.Min(lines.Length, i + afterContext + 1);
					for (int ctx = i + 1; ctx < contextEnd; ctx++)
					{
						if (!matches.Contains($"{ctx + 1}: {lines[ctx]}"))
						{
							if (showLineNumbers)
								matches.Add($"{ctx + 1}: {lines[ctx]}");
							else
								matches.Add(lines[ctx]);
						}
					}
				}

				count++;
				if (count >= limitLinesPerFile)
					break;
			}

			return matches;
		}
	}
}