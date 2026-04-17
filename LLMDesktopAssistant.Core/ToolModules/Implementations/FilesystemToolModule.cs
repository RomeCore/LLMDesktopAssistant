using DocumentFormat.OpenXml.Bibliography;
using LLMDesktopAssistant.Core.LLM.Domain;
using LLMDesktopAssistant.Core.LLM.Services.Attachments;
using LLMDesktopAssistant.Core.ToolModules;
using LLMDesktopAssistant.Core.Utils.Files;
using RCLargeLanguageModels.Tools;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;
using System.Text;
using System.Text.RegularExpressions;

namespace LLMDesktopAssistant.Core.ToolModules.Implementations
{
	[ToolModule]
	public class FilesystemToolModule : ToolModule
	{
		private readonly Chat _chat;
		private readonly IDocumentReadingService _documentReader;

		public FilesystemToolModule(Chat chat, IDocumentReadingService documentReader)
		{
			_chat = chat;
			_documentReader = documentReader;

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(GetFileInfo, "fs-get_file_info",
					"Returns file information including type classification."),
				Category = "filesystem",
				AskForConfirmation = false
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(ReadFile, "fs-read_file",
					"Reads text file content from the working directory."),
				Category = "filesystem",
				AskForConfirmation = false
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(ReadBinaryFile, "fs-read_binary_file",
					"Reads binary file content as hex dump from the working directory."),
				Category = "filesystem",
				AskForConfirmation = false
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(ReadDocumentFile, "fs-read_document_file",
					"Reads document file content by pages from the working directory. Supported extensions: .pdf, .docx, .pptx."),
				Category = "filesystem",
				AskForConfirmation = false
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(WriteFile, "fs-write_file",
					"Writes text content to a file inside working directory."),
				Category = "filesystem",
				AskForConfirmation = true
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(WriteBinaryFile, "fs-write_binary_file",
					"Writes binary content to a file inside working directory."),
				Category = "filesystem",
				AskForConfirmation = true
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(ApplyDiff, "fs-apply_diff",
					"""
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
					"""),
				Category = "filesystem",
				AskForConfirmation = true
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(ReplaceInFile, "fs-replace",
					"""
					Replaces all occurrences of a string or regex pattern in a text file line-by-line.
					Returns detailed information about applied changes including line numbers.
					"""),
				Category = "filesystem",
				AskForConfirmation = true
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(ListDirectory, "fs-list_directory",
					"Lists files and directories inside working directory path."),
				Category = "filesystem",
				AskForConfirmation = false
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(DeleteFile, "fs-delete_file",
					"Deletes a file inside working directory."),
				Category = "filesystem",
				AskForConfirmation = true
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(RenameFile, "fs-rename_file",
					"Renames or moves a file within the working directory."),
				Category = "filesystem",
				AskForConfirmation = true
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(CopyFile, "fs-copy_file",
					"Copies a file within the working directory."),
				Category = "filesystem",
				AskForConfirmation = true
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(OpenFile, "fs-open_file",
					"Opens a file from the working directory with its default application."),
				Category = "filesystem",
				AskForConfirmation = true
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(Grep, "fs-grep",
					"""
					Searches for pattern in files using regex.
					Use this tool with care, as it can be slow and resource-intensive.
					Use
					"""),
				Category = "filesystem",
				AskForConfirmation = false
			});
		}

		private string ResolvePath(string path)
		{
			var baseDir = Path.GetFullPath(_chat.Settings.GetWorkingDirectory());
			if (string.IsNullOrWhiteSpace(path) || path == ".")
				return baseDir;

			var fullPath = Path.GetFullPath(Path.Combine(baseDir, path));

			if (!fullPath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
				throw new AccessViolationException("Access outside working directory is not allowed.");

			return fullPath;
		}

		public ToolResult GetFileInfo(string path)
		{
			try
			{
				var fullPath = ResolvePath(path);

				var metrics = FileUtils.GetFileMetrics(fullPath);

				var additional = metrics.LineCount != null
					? $"Lines: {metrics.LineCount}"
					: "Binary file";

				var result = $"""
					Name: {metrics.Name}
					Type: {metrics.Type}
					Size: {metrics.Size} bytes
					{additional}
					Created: {metrics.Created:yyyy-MM-dd HH:mm:ss}
					Modified: {metrics.Modified:yyyy-MM-dd HH:mm:ss}
					Attributes: {metrics.Attributes}
					""";

				return new ToolResult(ToolResultStatus.Success, result);
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error getting file info: {ex.Message}");
			}
		}

		public ToolResult ReadFile(
			string path,
			[Description("The 1-based index of the first line to read.")]
			int startLine = 1,
			[Description("The 1-based index of the last line to read.")]
			int endLine = 300,
			[Description("The maximum length of each line to read.")]
			int maxLineLength = 2000,
			[Description("Whether to include line numbers before every line in format '   1: *line content*'.")]
			bool showLineNumbers = false)
		{
			try
			{
				var fullPath = ResolvePath(path);
				if (!File.Exists(fullPath))
					return new ToolResult(ToolResultStatus.Error, "File not found.");
				if (endLine < startLine)
					return new ToolResult(ToolResultStatus.Error, "Invalid line range.");

				var (lines, totalLines) = FileUtils.ReadLinesChunk(
					fullPath,
					startLine,
					endLine - startLine + 1,
					maxLineLength,
					showLineNumbers);

				if (lines.Count == 0)
					return new ToolResult(ToolResultStatus.Success, "No content.");

				var output = $"""
					File: {path}
					Total lines: {totalLines}
					Showing: {startLine}-{startLine + lines.Count - 1}
					Contents:
					{string.Join(Environment.NewLine, lines)}
					""";

				return new ToolResult(ToolResultStatus.Success, output);
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error reading file: {ex.Message}");
			}
		}

		public ToolResult ReadBinaryFile(
			string path,
			[Description("The 1-based index of the first byte to read.")]
			int startByte = 1,
			int endByte = 4096)
		{
			try
			{
				var fullPath = ResolvePath(path);
				if (!File.Exists(fullPath))
					return new ToolResult(ToolResultStatus.Error, "File not found.");
				if (endByte < startByte)
					return new ToolResult(ToolResultStatus.Error, "Invalid byte range.");

				var fileInfo = new FileInfo(fullPath);

				var (lines, read) = FileUtils.ReadHexChunk(
					fullPath,
					startByte,
					endByte - startByte + 1);

				if (read == 0)
					return new ToolResult(ToolResultStatus.Success, "No content.");

				var output = $"""
					File: {path}
					Size: {fileInfo.Length} bytes
					Showing bytes: {startByte}-{startByte + read - 1}
					Hex dump:
					{string.Join(Environment.NewLine, lines)}
					""";

				return new ToolResult(ToolResultStatus.Success, output);
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error reading binary file: {ex.Message}");
			}
		}

		public ToolResult ReadDocumentFile(
			string path,
			[Description("The 1-based index of the first page to read.")]
			int startPage = 1,
			[Description("The 1-based index of the last page to read.")]
			int endPage = 30)
		{
			try
			{
				var fullPath = ResolvePath(path);
				if (!File.Exists(fullPath))
					return new ToolResult(ToolResultStatus.Error, "File not found.");
				if (endPage < startPage)
					return new ToolResult(ToolResultStatus.Error, "Invalid page range.");

				var text = _documentReader.ExtractText(
					fullPath,
					startPage,
					endPage - startPage + 1);

				return new ToolResult(ToolResultStatus.Success, text);
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error reading document file: {ex.Message}");
			}
		}

		public ToolResult WriteFile(
			string path,
			string content,
			bool append = false)
		{
			try
			{
				var fullPath = ResolvePath(path);

				var dir = Path.GetDirectoryName(fullPath);
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir!);

				if (append)
					File.AppendAllText(fullPath, content);
				else
					File.WriteAllText(fullPath, content);

				return new ToolResult(ToolResultStatus.Success, "File written successfully.");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error writing file: {ex.Message}");
			}
		}

		public ToolResult WriteBinaryFile(
			string path,
			[Description("Hexadecimal string representing binary data in format '01 F8 2A'")]
			string hex,
			bool append = false)
		{
			try
			{
				var fullPath = ResolvePath(path);

				var dir = Path.GetDirectoryName(fullPath);
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir!);

				var hexClean = Regex.Replace(hex, @"[^0-9A-Fa-f]+", "");

				if (hexClean.Length % 2 != 0)
					return new ToolResult(ToolResultStatus.Error,
						"Invalid hex string length. Hex string must have an even number of characters.");

				var bytes = new byte[hexClean.Length / 2];
				for (int i = 0; i < bytes.Length; i++)
				{
					var hexByte = hexClean.Substring(i * 2, 2);
					if (!byte.TryParse(hexByte, System.Globalization.NumberStyles.HexNumber, null, out byte result))
						return new ToolResult(ToolResultStatus.Error,
							$"Invalid hex byte at position {i * 2}: '{hexByte}'");
					bytes[i] = result;
				}

				if (append)
				{
					using var fs = File.Open(fullPath, FileMode.Append);
					fs.Write(bytes, 0, bytes.Length);
				}
				else
				{
					File.WriteAllBytes(fullPath, bytes);
				}

				return new ToolResult(ToolResultStatus.Success, "File written successfully.");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error writing binary file: {ex.Message}");
			}
		}

		public ToolResult ApplyDiff(
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
				var fullPath = ResolvePath(path);

				if (!File.Exists(fullPath))
					return new ToolResult(ToolResultStatus.Error, "File not found.");

				if (FileUtils.IsBinaryFile(fullPath))
					return new ToolResult(ToolResultStatus.Error, "Cannot apply diff to binary files.");

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
						return new ToolResult(ToolResultStatus.Error,
							$"Start line {startLine} is out of range. File has {lines.Count} lines.");

					if (endLine < startLine)
						return new ToolResult(ToolResultStatus.Error,
							$"End line {endLine} must be greater than or equal to start line {startLine}");

					if (endLine > lines.Count)
						return new ToolResult(ToolResultStatus.Error,
							$"End line {endLine} is out of range. File has {lines.Count} lines.");
				}

				if (insertAtLine != null && insertText != null)
				{
					if (insertAtLine < 1)
						return new ToolResult(ToolResultStatus.Error,
							$"Line number {insertAtLine} must be at least 1");

					if (insertAtLine > lines.Count + 1)
						return new ToolResult(ToolResultStatus.Error,
							$"Line number {insertAtLine} is out of range. File has {lines.Count} lines. " +
							$"Max insert position is {lines.Count + 1}");
				}
				else if (insertAtLine != null && insertText == null)
				{
					return new ToolResult(ToolResultStatus.Error,
						"insertText parameter is required when insertAtLine is specified");
				}
				else if (insertText != null && insertAtLine == null)
				{
					return new ToolResult(ToolResultStatus.Error,
						"insertAtLine parameter is required when insertText is specified");
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
					return new ToolResult(ToolResultStatus.Success,
						"No changes applied to the file.");
				}

				File.WriteAllText(fullPath, newContent);

				var changeReport = BuildChangeReport(
					beforeDeletionLines,
					beforeInsertionLines,
					lines,
					deletedStartLine, deletedEndLine, deletedContent,
					insertedStartLine, insertedEndLine, insertedContent,
					path);

				return new ToolResult(ToolResultStatus.Success, changeReport);
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error applying diff: {ex.Message}");
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
					int startIdx = Math.Max(0, deletedContent.Count - 5);
					for (int i = startIdx; i < deletedContent.Count; i++)
					{
						report.AppendLine($"> {deletedStartLine + i,6}: {deletedContent[i]}");
					}
				}
				report.AppendLine();

				int afterStart = Math.Min(beforeDeletionLines.Count - 1, deletedEndLine);
				int afterEnd = Math.Min(beforeDeletionLines.Count, deletedEndLine + 5);
				if (afterStart < afterEnd)
				{
					for (int i = afterStart; i < afterEnd; i++)
					{
						report.AppendLine($"{i + 1,6}: {beforeDeletionLines[i]}");
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
					int startIdx = Math.Max(0, insertedContent.Count - 5);
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

		public ToolResult ReplaceInFile(
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
				var fullPath = ResolvePath(path);

				if (!File.Exists(fullPath))
					return new ToolResult(ToolResultStatus.Error, "File not found.");

				if (FileUtils.IsBinaryFile(fullPath))
					return new ToolResult(ToolResultStatus.Error, "Cannot replace text in binary files.");

				if (string.IsNullOrEmpty(oldString) && string.IsNullOrEmpty(oldRegex))
					return new ToolResult(ToolResultStatus.Error, "Both 'oldString' and 'oldRegex' parameters cannot be null or empty.");

				var content = File.ReadAllText(fullPath);
				var lines = content.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);

				var changes = new List<(int lineNumber, string oldLine, string newLine)>();
				var newLines = new List<string>();
				var totalReplacements = 0;

				var regex = oldString != null ?
					new Regex(Regex.Escape(oldString), RegexOptions.Compiled) :
					new Regex(oldRegex!, RegexOptions.Compiled);
				newString ??= string.Empty;

				for (int i = 0; i < lines.Length; i++)
				{
					var line = lines[i];
					var lineNumber = i + 1;

					var matches = regex.Matches(line);
					if (matches.Count > 0)
					{
						var newLine = regex.Replace(line, newString);
						totalReplacements += matches.Count;

						changes.Add((lineNumber, line, newLine));
						newLines.Add(newLine);
					}
					else
					{
						newLines.Add(line);
					}
				}

				if (totalReplacements == 0)
				{
					return new ToolResult(ToolResultStatus.Success,
						$"No occurrences of '{oldString}' found in file '{path}'.");
				}

				var newContent = string.Join(Environment.NewLine, newLines);
				File.WriteAllText(fullPath, newContent);

				var report = new StringBuilder();
				var oldPattern = oldString ?? oldRegex!;
				report.AppendLine($"Successfully replaced '{oldPattern}' with '{newString ?? "(empty)"}' in file '{path}'");
				report.AppendLine($"Summary:");
				report.AppendLine($"  Total lines modified: {changes.Count}");
				report.AppendLine($"  Total replacements: {totalReplacements}");
				report.AppendLine($"  File size before: {content.Length} bytes");
				report.AppendLine($"  File size after: {newContent.Length} bytes");

				if (changes.Count <= 10) // Show all changes if 10 or less
				{
					report.AppendLine();
					report.AppendLine($"Detailed changes:");
					foreach (var change in changes)
					{
						report.AppendLine($"  Line {change.lineNumber}:");
						report.AppendLine($"    Before: {Truncate(change.oldLine, 80)}");
						report.AppendLine($"    After:  {Truncate(change.newLine, 80)}");
						report.AppendLine();
					}
				}
				else // Show only first 5 and last 5 changes
				{
					report.AppendLine();
					report.AppendLine($"First 5 changes:");
					for (int i = 0; i < Math.Min(5, changes.Count); i++)
					{
						var change = changes[i];
						report.AppendLine($"  Line {change.lineNumber}: {Truncate(change.oldLine, 60)} → {Truncate(change.newLine, 60)}");
					}

					if (changes.Count > 10)
					{
						report.AppendLine($"   ... and {changes.Count - 10} more lines");
					}

					if (changes.Count > 5)
					{
						report.AppendLine($"📝 Last 5 changes:");
						for (int i = Math.Max(5, changes.Count - 5); i < changes.Count; i++)
						{
							var change = changes[i];
							report.AppendLine($"  Line {change.lineNumber}: {Truncate(change.oldLine, 60)} → {Truncate(change.newLine, 60)}");
						}
					}
				}

				return new ToolResult(ToolResultStatus.Success, report.ToString());
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error replacing text in file: {ex.Message}");
			}
		}

		private string Truncate(string text, int maxLength)
		{
			if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
				return text;

			return text.Substring(0, maxLength - 3) + "...";
		}

		public ToolResult DeleteFile(string path)
		{
			try
			{
				var fullPath = ResolvePath(path);

				if (!File.Exists(fullPath))
					return new ToolResult(ToolResultStatus.Error, "File not found.");

				File.Delete(fullPath);

				return new ToolResult(ToolResultStatus.Success, "File deleted.");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error deleting file: {ex.Message}");
			}
		}

		public ToolResult RenameFile(
			string oldPath,
			string newPath,
			bool overwrite = false)
		{
			try
			{
				var fullOldPath = ResolvePath(oldPath);
				var fullNewPath = ResolvePath(newPath);

				if (!File.Exists(fullOldPath))
					return new ToolResult(ToolResultStatus.Error, "Source file not found.");

				if (File.Exists(fullNewPath))
					return new ToolResult(ToolResultStatus.Error, "Destination file already exists.");

				if (Path.GetDirectoryName(fullNewPath) is string dir)
					Directory.CreateDirectory(dir);
				File.Move(fullOldPath, fullNewPath, overwrite);

				return new ToolResult(ToolResultStatus.Success, $"File renamed from '{oldPath}' to '{newPath}'.");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error renaming file: {ex.Message}");
			}
		}

		public ToolResult CopyFile(
			string oldPath,
			string newPath,
			bool overwrite = false)
		{
			try
			{
				var fullOldPath = ResolvePath(oldPath);
				var fullNewPath = ResolvePath(newPath);

				if (!File.Exists(fullOldPath))
					return new ToolResult(ToolResultStatus.Error, "Source file not found.");

				if (File.Exists(fullNewPath) && !overwrite)
					return new ToolResult(ToolResultStatus.Error, "Destination file already exists.");

				if (Path.GetDirectoryName(fullNewPath) is string dir)
					Directory.CreateDirectory(dir);
				File.Copy(fullOldPath, fullNewPath, overwrite);

				return new ToolResult(ToolResultStatus.Success, $"File copied from '{oldPath}' to '{newPath}'.");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error copying file: {ex.Message}");
			}
		}

		public async Task<ToolResult> OpenFile(string filename)
		{
			try
			{
				var workDir = _chat.Settings.GetWorkingDirectory();
				var fullPath = Path.Combine(workDir, filename);

				if (!File.Exists(fullPath))
				{
					return new ToolResult(ToolResultStatus.Error,
						$"File not found: {filename}");
				}

				using (Process process = new Process())
				{
					process.StartInfo = new ProcessStartInfo
					{
						FileName = fullPath,
						WorkingDirectory = workDir,
						UseShellExecute = true
					};
					process.Start();
				}

				return new ToolResult(ToolResultStatus.Success,
					$"Successfully opened: {filename}");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error,
					$"Error opening file {filename}: {ex.Message}");
			}
		}

		public ToolResult ListDirectory(
			string path = "",
			[Description("The maximum depth of directories to include in the listing. 1 = current directory only.")]
			int maxDepth = 1,
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
				var fullPath = ResolvePath(path);
				if (!Directory.Exists(fullPath))
					return new ToolResult(ToolResultStatus.Error, "Directory not found.");

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
								var fileInfo = new FileInfo(entry);
								var sizeStr = FileUtils.BytesToDisplaySize(fileInfo.Length);
								var edited = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm");

								sb.AppendLine($"{indent}[FILE] {name} ({sizeStr}, {edited})");
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

				return new ToolResult(ToolResultStatus.Success, sb.ToString());
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error listing directory: {ex.Message}");
			}
		}

		public ToolResult Grep(
			[Description("The regex pattern to search for.")]
			string pattern,
			[Description("The path where to search, can be file or directory path.")]
			string path,
			[Description("The file extensions to allow in search. Examples: '.cs', '.txt' etc.")]
			string[]? allowedExtensions,
			[Description("The maximum count of matched files to return.")]
			int limitFiles = 20,
			[Description("The maximum count of matched lines per file to return.")]
			int limitLinesPerFile = 10,
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
			bool recursive = false,
			CancellationToken cancellationToken = default)
		{
			try
			{
				var workingDirectory = _chat.Settings.GetWorkingDirectory();
				var fullPath = ResolvePath(path);
				var regexIgnoreCaseOptions = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
				var regex = new Regex(pattern, regexIgnoreCaseOptions | RegexOptions.Compiled);

				var filesToSearch = new List<string>();
				var results = new List<string>();
				int totalFilesMatched = 0;

				if (File.Exists(fullPath))
				{
					filesToSearch.Add(fullPath);
				}
				else if (Directory.Exists(fullPath))
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
								filesToSearch.Add(file);
						}
						catch (Exception ex)
						{
							results.Add($"Error checking file {file}: {ex.Message}");
						}
					}
				}
				else
				{
					return new ToolResult(ToolResultStatus.Error, "File or directory not found.");
				}

				foreach (var file in filesToSearch)
				{
					cancellationToken.ThrowIfCancellationRequested();

					var relativePath = Path.GetRelativePath(workingDirectory, file);
					var fileMatches = SearchInFile(workingDirectory, file, regex, limitLinesPerFile, invert, onlyMatching,
													lineNumbers, beforeContext, afterContext, cancellationToken);
					if (fileMatches.Count > 0)
					{
						results.Add($"\n--- {relativePath} ---");
						results.AddRange(fileMatches);

						totalFilesMatched++;
						if (totalFilesMatched >= limitFiles)
							break;
					}
				}

				if (results.Count == 0)
					return new ToolResult(ToolResultStatus.Success, "No matches found.");

				return new ToolResult(ToolResultStatus.Success, string.Join("\n", results));
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Grep error: {ex.Message}");
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