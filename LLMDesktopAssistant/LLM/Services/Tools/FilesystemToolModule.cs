using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.ToolModules;
using LLMDesktopAssistant.Utils.Files;
using RCLargeLanguageModels.Tools;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace LLMDesktopAssistant.LLM.Services.Tools
{
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
					"Applies diff operations to a file. Supports deleting a range of lines and/or inserting text at a specific line."),
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
				Tool = FunctionTool.From(OpenFile, "fs-open_file",
					"Opens a file from the working directory with its default application."),
				Category = "filesystem",
				AskForConfirmation = true
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(Grep, "fs-grep",
					"Searches for pattern in files using regex."),
				Category = "filesystem",
				AskForConfirmation = false
			});
		}

		private string ResolvePath(string path)
		{
			var baseDir = Path.GetFullPath(_chat.Settings.GetWorkingDirectory());
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
			int lineStart = 1,
			int lineCount = 300,
			int maxLineLength = 2000,
			bool showLineNumbers = false)
		{
			try
			{
				var fullPath = ResolvePath(path);

				if (!File.Exists(fullPath))
					return new ToolResult(ToolResultStatus.Error, "File not found.");

				var (lines, totalLines) = FileUtils.ReadLinesChunk(
					fullPath,
					lineStart,
					lineCount,
					maxLineLength,
					showLineNumbers);

				if (lines.Count == 0)
					return new ToolResult(ToolResultStatus.Success, "No content.");

				var endLine = lineStart + lines.Count - 1;

				var output = $"""
					File: {path}
					Total lines: {totalLines}
					Showing: {lineStart}-{endLine}
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
			int bytesCount = 4096)
		{
			try
			{
				var fullPath = ResolvePath(path);

				if (!File.Exists(fullPath))
					return new ToolResult(ToolResultStatus.Error, "File not found.");

				var fileInfo = new FileInfo(fullPath);

				var (lines, read) = FileUtils.ReadHexChunk(fullPath, startByte, bytesCount);

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
			int startPage = 1,
			int pageCount = 30)
		{
			try
			{
				var fullPath = ResolvePath(path);

				if (!File.Exists(fullPath))
					return new ToolResult(ToolResultStatus.Error, "File not found.");

				var text = _documentReader.ExtractText(fullPath, startPage, pageCount);

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
			int? insertAtLine = null,
			string? insertText = null)
		{
			try
			{
				var fullPath = ResolvePath(path);

				if (!File.Exists(fullPath))
					return new ToolResult(ToolResultStatus.Error, "File not found.");

				var originalContent = File.ReadAllText(fullPath);
				var lines = originalContent.Split(["\r\n", "\n", "\r"], StringSplitOptions.None).ToList();

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
					if (insertAtLine < 1)
						return new ToolResult(ToolResultStatus.Error,
							$"Line number {insertAtLine} must be at least 1");

					if (insertAtLine > lines.Count + 1)
						return new ToolResult(ToolResultStatus.Error,
							$"Line number {insertAtLine} is out of range. File has {lines.Count} lines. " +
							$"Max insert position is {lines.Count + 1}");

					int insertPosition = insertAtLine.Value - 1;
					var insertLines = insertText.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
					lines.InsertRange(insertPosition, insertLines);
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

				var newContent = string.Join(Environment.NewLine, lines);
				if (newContent == originalContent)
				{
					return new ToolResult(ToolResultStatus.Success,
						"No changes applied to the file.");
				}

				File.WriteAllText(fullPath, newContent);
				return new ToolResult(ToolResultStatus.Success, "Changes applied successfully.");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error applying diff: {ex.Message}");
			}
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

		public ToolResult ListDirectory(
			string path = "",
			int maxDepth = 2,
			int maxEntries = 200)
		{
			try
			{
				var fullPath = ResolvePath(path);

				if (!Directory.Exists(fullPath))
					return new ToolResult(ToolResultStatus.Error, "Directory not found.");

				var sb = new StringBuilder();
				int totalCount = 0;
				bool truncated = false;

				void Traverse(string currentPath, int depth, string indent)
				{
					if (depth > maxDepth || truncated)
						return;

					var entries = Directory.GetFileSystemEntries(currentPath)
						.OrderBy(e => Directory.Exists(e) ? 0 : 1)
						.ThenBy(e => e);

					foreach (var entry in entries)
					{
						if (totalCount >= maxEntries)
						{
							truncated = true;
							return;
						}

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

							var edited = Directory.GetLastWriteTime(entry).ToString("yyyy-MM-dd HH:mm");

							sb.AppendLine($"{indent}[DIR]  {name} ({count} items, {edited})");

							totalCount++;

							Traverse(entry, depth + 1, indent + "  ");
						}
						else
						{
							var fileInfo = new FileInfo(entry);
							var sizeStr = FileUtils.BytesToDisplaySize(fileInfo.Length);
							var edited = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm");

							sb.AppendLine($"{indent}[FILE] {name} ({sizeStr}, {edited})");

							totalCount++;
						}
					}
				}

				sb.AppendLine($"[DIRECTORY LISTING]");
				sb.AppendLine($"Path: {path}");
				sb.AppendLine($"MaxDepth: {maxDepth}, MaxEntries: {maxEntries}");
				sb.AppendLine();

				Traverse(fullPath, 0, "");

				if (truncated)
				{
					sb.AppendLine();
					sb.AppendLine($"... truncated (limit {maxEntries} entries reached)");
				}

				return new ToolResult(ToolResultStatus.Success, sb.ToString());
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error listing directory: {ex.Message}");
			}
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

		public ToolResult Grep(
			string pattern,
			string path,
			bool ignoreCase = false,
			bool lineNumbers = false,
			bool invert = false,
			bool onlyMatching = false,
			int beforeContext = 0,
			int afterContext = 0,
			bool recursive = false,
			[Description("The file extensions to include in search. Examples: '.cs', '.txt' etc.")]
			string[]? includeExtensions = null)
		{
			try
			{
				var fullPath = ResolvePath(path);
				var regexOptions = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
				var regex = new Regex(pattern, regexOptions);

				var filesToSearch = new List<string>();

				if (File.Exists(fullPath))
				{
					filesToSearch.Add(fullPath);
				}
				else if (Directory.Exists(fullPath))
				{
					if (!recursive)
						return new ToolResult(ToolResultStatus.Error,
							"Path is a directory. Use recursive=true to search inside.");

					var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
					var allFiles = Directory.GetFiles(fullPath, "*.*", searchOption);

					foreach (var file in allFiles)
					{
						if (includeExtensions != null && includeExtensions.Length > 0)
						{
							var ext = Path.GetExtension(file);
							if (!includeExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
								continue;
						}

						// Skip binary files automatically
						if (!FileUtils.IsBinaryFile(file))
							filesToSearch.Add(file);
					}
				}
				else
				{
					return new ToolResult(ToolResultStatus.Error, "File or directory not found.");
				}

				var results = new List<string>();

				foreach (var file in filesToSearch)
				{
					var fileMatches = SearchInFile(file, regex, invert, onlyMatching,
													lineNumbers, beforeContext, afterContext);
					if (fileMatches.Count > 0)
					{
						if (filesToSearch.Count > 1)
							results.Add($"\n--- {file} ---");
						results.AddRange(fileMatches);
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
			string filePath,
			Regex regex,
			bool invert,
			bool onlyMatching,
			bool showLineNumbers,
			int beforeContext,
			int afterContext)
		{
			var lines = File.ReadAllLines(filePath);
			var matches = new List<string>();

			for (int i = 0; i < lines.Length; i++)
			{
				var line = lines[i];
				var isMatch = regex.IsMatch(line);

				if (invert) isMatch = !isMatch;

				if (isMatch)
				{
					// Add context lines before
					if (beforeContext > 0)
					{
						int contextStart = Math.Max(0, i - beforeContext);
						for (int ctx = contextStart; ctx < i; ctx++)
						{
							if (!matches.Contains($"{ctx + 1}-ctx: {lines[ctx]}"))
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
							if (!matches.Contains($"{ctx + 1}-ctx: {lines[ctx]}"))
							{
								if (showLineNumbers)
									matches.Add($"{ctx + 1}: {lines[ctx]}");
								else
									matches.Add(lines[ctx]);
							}
						}
					}
				}
			}

			return matches;
		}
	}
}