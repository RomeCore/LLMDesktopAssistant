using System.ComponentModel;
using System.Text.RegularExpressions;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils.Files;
using UglyToad.PdfPig.Graphics.Operations.PathPainting;

namespace LLMDesktopAssistant.Tools.Implementations.Filesystem
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
					Description = "Reads complex documents (DOCX, PPTX, PDF) by pages from the working directory. Supported extensions: .pdf, .docx, .pptx. This is not suitable for general text or code files, such as .txt, .py, .md, .cs, .js, etc.",
					Category = "filesystem",
					AskForConfirmation = false
				});

			AddTool(WriteBinaryFile,
				new ToolInitializationInfo
				{
					Name = "fs-write_binary_file",
					Description = "Writes binary content to a file inside working directory.",
					Category = "filesystem",
					AskForConfirmation = true
				});

			AddTool(CreateDirectory,
				new ToolInitializationInfo
				{
					Name = "fs-create_directory",
					Description = "Creates a new directory inside working directory path.",
					Category = "filesystem",
					AskForConfirmation = true
				});

			AddTool(DeleteFile, null, PreviewDeleteFile,
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
		}

		public ReactiveToolResult GetFileInfo(string path)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);
				var metrics = FileUtils.GetFileMetrics(fullPath);

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
					StatusTitle = $"**{path}** *({FileUtils.BytesToDisplaySize(metrics.Size)})*"
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

		public ReactiveToolResult ReadBinaryFile(
			string path,
			[Description("The 1-based index of the first byte to read.")]
			int startByte = 1,
			int endByte = 4096)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);

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
						(startByte == 1 ? $"**{path}**" : $"**{path}** *({startByte}~{endShown})*") :
						$"**{path}** *({startByte}~{endShown} / {totalBytes})*"
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
					StatusTitle = $"**{path}** *({startPage}~{endPage})*"
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

		public ReactiveToolResult WriteBinaryFile(
			string path,
			[Description("Hexadecimal string representing binary data in format '01 F8 2A'")]
			string hex,
			bool append = false)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);
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
					StatusTitle = $"**{path}**"
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

		public ReactiveToolResult CreateDirectory(string path)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);

				if (Directory.Exists(fullPath))
					return ReactiveToolResult.CreateError("Directory already exists.");

				Directory.CreateDirectory(fullPath);

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.FolderPlus,
					StatusTitle = $"**{path}**",
					ResultContent = $"Directory '{path}' created successfully."
				};

				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error creating directory: {ex.Message}");
			}
		}

		public PreviewToolExecutionResult PreviewDeleteFile(string path)
		{
			var fullPath = _fileAccess.TryAccessPath(path);
			var fileName = Path.GetFileName(fullPath);

			return new PreviewToolExecutionResult
			{
				StatusIcon = Material.Icons.MaterialIconKind.Delete,
				StatusTitle = $"**{fileName}**",
				DangerLevel = ToolDangerLevel.Warning
			};
		}

		public ReactiveToolResult DeleteFile(string path)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);

				if (!File.Exists(fullPath))
					return ReactiveToolResult.CreateError("File not found.");

				var fileInfo = new FileInfo(fullPath);
				var size = FileUtils.BytesToDisplaySize(fileInfo.Length);

				File.Delete(fullPath);

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.Delete,
					StatusTitle = $"**{path}**",
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

				if (!Directory.Exists(fullPath))
					return ReactiveToolResult.CreateError("Directory not found.");

				var dirInfo = new DirectoryInfo(fullPath);

				Directory.Delete(fullPath, recursive: true);

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.FolderRemove,
					StatusTitle = $"**{path}**",
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
					StatusTitle = $"**{oldPath}** → **{newPath}**",
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
			string oldPath,
			string newPath,
			bool overwrite = false)
		{
			try
			{
				var fullOldPath = _fileAccess.AccessPath(oldPath);
				var fullNewPath = _fileAccess.AccessPath(newPath);

				if (!Directory.Exists(fullOldPath))
					return ReactiveToolResult.CreateError("Source directory not found.");

				if (Directory.Exists(fullNewPath) && !overwrite)
					return ReactiveToolResult.CreateError("Destination directory already exists.");

				Directory.CreateDirectory(Path.GetDirectoryName(fullNewPath)!);
				Directory.Move(fullOldPath, fullNewPath);

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.Folder,
					StatusTitle = $"**{oldPath}** → **{newPath}**",
					ResultContent = $"Directory moved from '{oldPath}' to '{newPath}'."
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
					StatusTitle = $"**{oldPath}** → **{newPath}**",
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
			string oldPath,
			string newPath,
			bool overwrite = false)
		{
			try
			{
				var fullOldPath = _fileAccess.AccessPath(oldPath);
				var fullNewPath = _fileAccess.AccessPath(newPath);

				if (!Directory.Exists(fullOldPath))
					return ReactiveToolResult.CreateError("Source directory not found.");

				if (Directory.Exists(fullNewPath) && !overwrite)
					return ReactiveToolResult.CreateError("Destination directory already exists.");

				Directory.CreateDirectory(fullNewPath);

				foreach (var file in Directory.GetFiles(fullOldPath, "*", SearchOption.AllDirectories))
				{
					var relativePath = Path.GetRelativePath(fullOldPath, file);
					var destFile = Path.Combine(fullNewPath, relativePath);
					Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
					File.Copy(file, destFile, overwrite);
				}

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.ContentCopy,
					StatusTitle = $"**{oldPath}** → **{newPath}**",
					ResultContent = $"Directory copied from '{oldPath}' to '{newPath}'."
				};

				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error copying directory: {ex.Message}");
			}
		}
	}
}