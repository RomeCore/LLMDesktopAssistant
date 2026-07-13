using System.ComponentModel;
using System.Text.RegularExpressions;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils.Files;
using Material.Icons;
using SixLabors.ImageSharp.PixelFormats;

namespace LLMDesktopAssistant.Tools.Implementations.Filesystem
{
	[ToolModule]
	public class FilesystemToolModule : ToolModule
	{
		private readonly WorkingDirectoryAccessService _fileAccess;
		private readonly IDocumentReadingService _documentReader;

		public FilesystemToolModule(WorkingDirectoryAccessService fileAccess, IDocumentReadingService documentReader)
		{
			_fileAccess = fileAccess;
			_documentReader = documentReader;

			AddTool(GetFileInfo, null, PreviewGetFileInfo,
				new ToolInitializationInfo
				{
					Name = "fs-get_file_info",
					Description = "Returns file information including type classification.",
					Category = "filesystem",
					DefaultExpectedBehaviour = ToolBehaviour.FileRead | ToolBehaviour.AccessOutsideWorkdir
				});

			AddTool(ReadBinaryFile, null, PreviewReadBinaryFile,
				new ToolInitializationInfo
				{
					Name = "fs-read_binary_file",
					Description = "Reads binary file content as hex dump from the working directory.",
					Category = "filesystem",
					DefaultExpectedBehaviour = ToolBehaviour.FileRead | ToolBehaviour.AccessOutsideWorkdir
				});

			AddTool(ReadDocumentFile, null, PreviewReadDocumentFile,
				new ToolInitializationInfo
				{
					Name = "fs-read_document_file",
					Description = "Reads complex documents (DOCX, PPTX, PDF) by pages from the working directory. Supported extensions: .pdf, .docx, .pptx. This is not suitable for general text or code files, such as .txt, .py, .md, .cs, .js, etc.",
					Category = "filesystem",
					DefaultExpectedBehaviour = ToolBehaviour.FileRead | ToolBehaviour.AccessOutsideWorkdir
				});

			AddTool(WriteBinaryFile, null, PreviewWriteBinaryFile,
				new ToolInitializationInfo
				{
					Name = "fs-write_binary_file",
					Description = "Writes binary content to a file inside working directory.",
					Category = "filesystem",
					DefaultExpectedBehaviour = ToolBehaviour.FileDirectoryCreate | ToolBehaviour.FileEdit | ToolBehaviour.AccessOutsideWorkdir
				});

			AddTool(CreateDirectory, null, PreviewCreateDirectory,
				new ToolInitializationInfo
				{
					Name = "fs-create_directory",
					Description = "Creates a new directory inside working directory path.",
					Category = "filesystem",
					DefaultExpectedBehaviour = ToolBehaviour.FileDirectoryCreate | ToolBehaviour.AccessOutsideWorkdir
				});

			AddTool(DeleteFile, null, PreviewDeleteFile,
				new ToolInitializationInfo
				{
					Name = "fs-delete_file",
					Description = "Deletes a file inside working directory.",
					Category = "filesystem",
					DefaultExpectedBehaviour = ToolBehaviour.FileDelete | ToolBehaviour.AccessOutsideWorkdir
				});

			AddTool(DeleteDirectory, null, PreviewDeleteDirectory,
				new ToolInitializationInfo
				{
					Name = "fs-delete_directory",
					Description = "Deletes a directory (empty or with contents) from the working directory.",
					Category = "filesystem",
					DefaultExpectedBehaviour = ToolBehaviour.DirectoryDelete | ToolBehaviour.FileDelete | ToolBehaviour.AccessOutsideWorkdir
				});

			AddTool(CopyFile, null, PreviewCopyFile,
				new ToolInitializationInfo
				{
					Name = "fs-copy_file",
					Description = "Copies a file within the working directory.",
					Category = "filesystem",
					DefaultExpectedBehaviour = ToolBehaviour.FileDirectoryCreate | ToolBehaviour.AccessOutsideWorkdir
				});

			AddTool(CopyDirectory, null, PreviewCopyDirectory,
				new ToolInitializationInfo
				{
					Name = "fs-copy_directory",
					Description = "Copies a directory and all its contents to a new location within the working directory.",
					Category = "filesystem",
					DefaultExpectedBehaviour = ToolBehaviour.FileDirectoryCreate | ToolBehaviour.AccessOutsideWorkdir
				});

			AddTool(RenameFile, null, PreviewRenameFile,
				new ToolInitializationInfo
				{
					Name = "fs-rename_file",
					Description = "Renames or moves a file within the working directory.",
					Category = "filesystem",
					DefaultExpectedBehaviour = ToolBehaviour.FileEdit | ToolBehaviour.AccessOutsideWorkdir
				});

			AddTool(MoveDirectory, null, PreviewMoveDirectory,
				new ToolInitializationInfo
				{
					Name = "fs-move_directory",
					Description = "Moves a directory and all its contents to a new location within the working directory.",
					Category = "filesystem",
					DefaultExpectedBehaviour = ToolBehaviour.DirectoryEdit | ToolBehaviour.AccessOutsideWorkdir
				});
		}

		public PreviewToolExecutionResult PreviewGetFileInfo(string path, [SharedContext] out string fullPath)
		{
			fullPath = _fileAccess.CheckedAccessPath(path, out var isAccessed);

			if (!File.Exists(fullPath))
			{
				new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FileQuestion,
					StatusTitle = $"**{path}**",
					ExpectedBehaviour = !isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = false,
					InterruptingContent = $"File not found: {path}"
				};
			}

			var metrics = FileUtils.GetFileMetrics(fullPath);
			return new PreviewToolExecutionResult
			{
				StatusIcon = metrics.Type switch
				{
					FileType.Binary => MaterialIconKind.File,
					FileType.Text => MaterialIconKind.FileText,
					FileType.Code => MaterialIconKind.FileCode,
					FileType.Image => MaterialIconKind.FileImage,
					FileType.Audio => MaterialIconKind.FileMusic,
					FileType.Video => MaterialIconKind.FileVideo,
					FileType.Executable => MaterialIconKind.Application,
					FileType.Archive => MaterialIconKind.Archive,
					FileType.Document => MaterialIconKind.FileDocument,
					_ => MaterialIconKind.FileQuestion
				},
				StatusTitle = $"**{path}** *({FileUtils.BytesToDisplaySize(metrics.Size)})*",
				ExpectedBehaviour = ToolBehaviour.FileRead | (!isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None)
			};
		}

		public ReactiveToolResult GetFileInfo(string path, [SharedContext] string? fullPath = null)
		{
			try
			{
				fullPath ??= _fileAccess.AccessPath(path);
				var metrics = FileUtils.GetFileMetrics(fullPath);

				var result = new ReactiveToolResult
				{
					StatusIcon = metrics.Type switch
					{
						FileType.Binary => MaterialIconKind.File,
						FileType.Text => MaterialIconKind.FileText,
						FileType.Code => MaterialIconKind.FileCode,
						FileType.Image => MaterialIconKind.FileImage,
						FileType.Audio => MaterialIconKind.FileMusic,
						FileType.Video => MaterialIconKind.FileVideo,
						FileType.Executable => MaterialIconKind.Application,
						FileType.Archive => MaterialIconKind.Archive,
						FileType.Document => MaterialIconKind.FileDocument,
						_ => MaterialIconKind.FileQuestion
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

		public PreviewToolExecutionResult PreviewReadBinaryFile(
			string path, [SharedContext] out string fullPath,
			int startByte = 1,
			int endByte = 4096)
		{
			fullPath = _fileAccess.CheckedAccessPath(path, out var isAccessed);

			if (!File.Exists(fullPath))
			{
				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FileQuestion,
					StatusTitle = $"**{path}**",
					ExpectedBehaviour = !isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = false,
					InterruptingContent = $"File not found: {path}"
				};
			}

			var expectedBehaviour = ToolBehaviour.FileRead | (!isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None);

			if (endByte < startByte)
			{
				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FileCode,
					StatusTitle = $"**{path}**",
					ExpectedBehaviour = expectedBehaviour,
					InterruptingSuccess = false,
					InterruptingContent = "Invalid byte range."
				};
			}
			else
			{
				var fileInfo = new FileInfo(fullPath);
				var totalBytes = fileInfo.Length;
				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FileCode,
					StatusTitle = startByte == 1 && endByte >= totalBytes
						? $"**{path}**"
						: $"**{path}** *({startByte}~{Math.Min(endByte, totalBytes)} / {totalBytes})*",
					ExpectedBehaviour = expectedBehaviour
				};
			}
		}

		public ReactiveToolResult ReadBinaryFile(
			string path,
			[Description("The 1-based index of the first byte to read.")]
			int startByte = 1,
			int endByte = 4096,
			[SharedContext] string? fullPath = null)
		{
			try
			{
				fullPath ??= _fileAccess.AccessPath(path);

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
					StatusIcon = MaterialIconKind.FileCode,
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

		public PreviewToolExecutionResult PreviewReadDocumentFile(
			string path, [SharedContext] out string fullPath,
			int startPage = 1,
			int endPage = 30)
		{
			fullPath = _fileAccess.CheckedAccessPath(path, out var isAccessed);

			if (!File.Exists(fullPath))
			{
				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FileQuestion,
					StatusTitle = $"**{path}**",
					ExpectedBehaviour = !isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = false,
					InterruptingContent = $"File not found: {path}"
				};
			}

			var expectedBehaviour = ToolBehaviour.FileRead | (!isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None);

			if (endPage < startPage)
			{
				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FileDocument,
					StatusTitle = $"**{path}**",
					ExpectedBehaviour = expectedBehaviour,
					InterruptingSuccess = false,
					InterruptingContent = "Invalid page range."
				};
			}

			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.FileDocument,
				StatusTitle = $"**{path}** *({startPage}~{endPage})*",
				ExpectedBehaviour = expectedBehaviour
			};
		}

		public ReactiveToolResult ReadDocumentFile(
			string path,
			[Description("The 1-based index of the first page to read.")]
			int startPage = 1,
			[Description("The 1-based index of the last page to read.")]
			int endPage = 30,
			[SharedContext] string? fullPath = null)
		{
			try
			{
				fullPath ??= _fileAccess.AccessPath(path);

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
					StatusIcon = MaterialIconKind.FileDocument,
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

		private static byte[] ParseHex(string hex)
		{
			var hexClean = Regex.Replace(hex, @"[^0-9A-Fa-f]+", "");

			if (hexClean.Length % 2 != 0)
				throw new ArgumentException("Invalid hex string length. Hex string must have an even number of characters.");

			var bytes = new byte[hexClean.Length / 2];
			for (int i = 0; i < bytes.Length; i++)
			{
				var hexByte = hexClean.Substring(i * 2, 2);
				if (!byte.TryParse(hexByte, System.Globalization.NumberStyles.HexNumber, null, out byte parsedByte))
					throw new ArgumentException($"Invalid hex byte at position {i * 2}: '{hexByte}'");
				bytes[i] = parsedByte;
			}

			return bytes;
		}

		public class WriteBinaryFileContext
		{
			public required string FullPath { get; init; }
			public required byte[] Bytes { get; init; }
		}

		public PreviewToolExecutionResult PreviewWriteBinaryFile(string path, string hex,
			[SharedContext] out WriteBinaryFileContext? ctx, bool append = false)
		{
			var fullPath = _fileAccess.CheckedAccessPath(path, out var isAccessed);
			bool fileExisted = File.Exists(fullPath);

			try
			{
				var bytes = ParseHex(hex);
				ctx = new WriteBinaryFileContext
				{
					FullPath = fullPath,
					Bytes = bytes
				};
				return new PreviewToolExecutionResult
				{
					StatusIcon = fileExisted ?
						(append ? MaterialIconKind.FileEdit : MaterialIconKind.FileCheck) :
						MaterialIconKind.FilePlus,
					StatusTitle = $"**{path}**",
					ExpectedBehaviour = (fileExisted ? ToolBehaviour.FileEdit : ToolBehaviour.FileDirectoryCreate) |
						(!isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None)
				};
			}
			catch (Exception ex)
			{
				ctx = null;
				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FileDocumentError,
					StatusTitle = $"**{path}**",
					ExpectedBehaviour = !isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = false,
					InterruptingContent = $"Error writing binary file: {ex.Message}"
				};
			}
		}

		public ReactiveToolResult WriteBinaryFile(
			string path,
			[Description("Hexadecimal string representing binary data in format '01 F8 2A'")]
			string hex,
			bool append = false,
			[SharedContext] WriteBinaryFileContext? ctx = null)
		{
			try
			{
				string fullPath;
				byte[] bytes;
				if (ctx != null)
				{
					fullPath = ctx.FullPath;
					bytes = ctx.Bytes;
				}
				else
				{
					fullPath = _fileAccess.AccessPath(path);
					bytes = ParseHex(hex);
				}

				var dir = Path.GetDirectoryName(fullPath);
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir!);
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

				return new ReactiveToolResult
				{
					StatusIcon = fileExisted ?
						(append ? MaterialIconKind.FileEdit : MaterialIconKind.FileCheck) :
						MaterialIconKind.FilePlus,
					StatusTitle = $"**{path}**",
					ResultContent = $"""
						File: {path}
						Operation: {(append ? "Append" : "Write")}
						Bytes written: {bytes.Length}
						Total size: {fileInfo.Length} bytes ~ ({size})
						"""
				}.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.FileDocumentError,
					StatusTitle = $"**{path}**",
					ResultContent = $"Error writing binary file: {ex.Message}"
				}.CompleteWithError();
			}
		}

		public PreviewToolExecutionResult PreviewCreateDirectory(string path, [SharedContext] out string fullPath)
		{
			fullPath = _fileAccess.CheckedAccessPath(path, out var isAccessed);

			if (Directory.Exists(fullPath))
			{
				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FolderQuestion,
					StatusTitle = $"**{path}**",
					ExpectedBehaviour = !isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = true,
					InterruptingContent = "Directory already exists."
				};
			}

			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.FolderPlus,
				StatusTitle = $"**{path}**",
				ExpectedBehaviour = (!Directory.Exists(fullPath) ? ToolBehaviour.FileDirectoryCreate : ToolBehaviour.None) |
					(!isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None)
			};
		}

		public ReactiveToolResult CreateDirectory(string path, [SharedContext] string? fullPath = null)
		{
			try
			{
				fullPath ??= _fileAccess.AccessPath(path);

				if (Directory.Exists(fullPath))
				{
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.FolderQuestion,
						StatusTitle = $"**{path}**",
						ResultContent = "Directory already exists."
					}.CompleteWithSuccess();
				}

				Directory.CreateDirectory(fullPath);

				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.FolderPlus,
					StatusTitle = $"**{path}**",
					ResultContent = $"Directory '{path}' created successfully."
				}.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.FolderPlus,
					StatusTitle = $"**{path}**",
					ResultContent = $"Error creating directory: {ex.Message}"
				}.CompleteWithError();
			}
		}

		public PreviewToolExecutionResult PreviewDeleteFile(string path, [SharedContext] out string? fullPath)
		{
			fullPath = _fileAccess.CheckedAccessPath(path, out var isAccessed);

			if (!File.Exists(fullPath))
			{
				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FileQuestion,
					StatusTitle = $"**{path}**",
					ExpectedBehaviour = !isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = false,
					InterruptingContent = $"File not found: {path}"
				};
			}

			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.Delete,
				StatusTitle = $"**{path}**",
				ExpectedBehaviour = ToolBehaviour.FileDelete |
					(!isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None)
			};
		}

		public ReactiveToolResult DeleteFile(string path, [SharedContext] string? fullPath = null)
		{
			try
			{
				fullPath ??= _fileAccess.AccessPath(path);

				if (!File.Exists(fullPath))
				{
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.FileQuestion,
						StatusTitle = $"**{path}**",
						ResultContent = $"File not found: {path}"
					}.CompleteWithSuccess();
				}

				var fileInfo = new FileInfo(fullPath);
				var size = FileUtils.BytesToDisplaySize(fileInfo.Length);

				File.Delete(fullPath);

				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.Delete,
					StatusTitle = $"**{path}**",
					ResultContent = $"File '{path}' deleted successfully."
				}.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.FileDocumentError,
					StatusTitle = $"**{path}**",
					ResultContent = $"Error deleting file: {ex.Message}"
				}.CompleteWithError();
			}
		}

		public class DeleteDirectoryContext
		{
			public required string FullPath { get; init; }
		}

		public PreviewToolExecutionResult PreviewDeleteDirectory(string path, [SharedContext] out DeleteDirectoryContext? ctx)
		{
			var fullPath = _fileAccess.CheckedAccessPath(path, out var isAccessed);
			ctx = new DeleteDirectoryContext { FullPath = fullPath };

			if (path == "." || path == "" || path == "/")
			{
				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FolderRemove,
					StatusTitle = $"**{path}**",
					ExpectedBehaviour = !isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = false,
					InterruptingContent = "Cannot delete the root working directory."
				};
			}

			if (!Directory.Exists(fullPath))
			{
				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FolderQuestion,
					StatusTitle = $"**{path}**",
					ExpectedBehaviour = !isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = false,
					InterruptingContent = $"Directory not found: {path}"
				};
			}

			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.FolderRemove,
				StatusTitle = $"**{path}**",
				ExpectedBehaviour = ToolBehaviour.DirectoryDelete |
					(Directory.GetFileSystemEntries(fullPath).Length > 0 ? ToolBehaviour.FileDelete : ToolBehaviour.None) |
					(!isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None)
			};
		}

		public ReactiveToolResult DeleteDirectory(string path, [SharedContext] DeleteDirectoryContext? ctx = null)
		{
			try
			{
				// Prevent deleting the root working directory
				if (path == "." || path == "" || path == "/")
				{
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.FolderRemove,
						StatusTitle = $"**{path}**",
						ResultContent = "Cannot delete the root working directory."
					}.CompleteWithError();
				}

				var fullPath = ctx?.FullPath ?? _fileAccess.AccessPath(path);

				if (!Directory.Exists(fullPath))
				{
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.FolderQuestion,
						StatusTitle = $"**{path}**",
						ResultContent = $"Directory not found: {path}"
					}.CompleteWithSuccess();
				}

				var dirInfo = new DirectoryInfo(fullPath);

				Directory.Delete(fullPath, recursive: true);

				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.FolderRemove,
					StatusTitle = $"**{path}**",
					ResultContent = $"Directory '{path}' deleted successfully."
				}.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.FolderRemove,
					StatusTitle = $"**{path}**",
					ResultContent = $"Error deleting directory: {ex.Message}"
				}.CompleteWithError();
			}
		}

		public class RenameFileContext
		{
			public required string FullOldPath { get; init; }
			public required string FullNewPath { get; init; }
		}

		public PreviewToolExecutionResult PreviewRenameFile(
			string oldPath,
			string newPath,
			[SharedContext] out RenameFileContext? ctx,
			bool overwrite = false)
		{
			var fullOldPath = _fileAccess.CheckedAccessPath(oldPath, out var isOldAccessed);
			var fullNewPath = _fileAccess.CheckedAccessPath(newPath, out var isNewAccessed);
			ctx = new RenameFileContext { FullOldPath = fullOldPath, FullNewPath = fullNewPath };

			if (!File.Exists(fullOldPath))
			{
				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FileQuestion,
					StatusTitle = $"**{oldPath}**",
					ExpectedBehaviour = !isOldAccessed || !isNewAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = false,
					InterruptingContent = "Source file not found."
				};
			}

			if (File.Exists(fullNewPath) && !overwrite)
			{
				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.PencilRemove,
					StatusTitle = $"**{oldPath}** → **{newPath}**",
					ExpectedBehaviour = !isOldAccessed || !isNewAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = false,
					InterruptingContent = "Destination file already exists."
				};
			}

			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.Pencil,
				StatusTitle = $"**{oldPath}** → **{newPath}**",
				ExpectedBehaviour = ToolBehaviour.FileEdit |
					(!isOldAccessed || !isNewAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None)
			};
		}

		public ReactiveToolResult RenameFile(
			string oldPath,
			string newPath,
			bool overwrite = false,
			[SharedContext] RenameFileContext? ctx = null)
		{
			try
			{
				var fullOldPath = ctx?.FullOldPath ?? _fileAccess.AccessPath(oldPath);
				var fullNewPath = ctx?.FullNewPath ?? _fileAccess.AccessPath(newPath);

				if (!File.Exists(fullOldPath))
				{
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.FileQuestion,
						StatusTitle = $"**{oldPath}** → **{newPath}**",
						ResultContent = $"Source file not found: {oldPath}"
					}.CompleteWithError();
				}

				if (File.Exists(fullNewPath) && !overwrite)
				{
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.PencilRemove,
						StatusTitle = $"**{oldPath}** → **{newPath}**",
						ResultContent = $"Destination file already exists: {newPath}"
					}.CompleteWithError();
				}

				if (Path.GetDirectoryName(fullNewPath) is string dir)
					Directory.CreateDirectory(dir);

				File.Move(fullOldPath, fullNewPath, overwrite);

				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.Pencil,
					StatusTitle = $"**{oldPath}** → **{newPath}**",
					ResultContent = $"File renamed from '{oldPath}' to '{newPath}'."
				}.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.PencilRemove,
					StatusTitle = $"**{oldPath}** → **{newPath}**",
					ResultContent = $"Error renaming file: {ex.Message}"
				}.CompleteWithError();
			}
		}

		public class MoveDirectoryContext
		{
			public required string FullOldPath { get; init; }
			public required string FullNewPath { get; init; }
		}

		public PreviewToolExecutionResult PreviewMoveDirectory(
			string oldPath,
			string newPath,
			[SharedContext] out MoveDirectoryContext? ctx,
			bool overwrite = false)
		{
			var fullOldPath = _fileAccess.CheckedAccessPath(oldPath, out var isOldAccessed);
			var fullNewPath = _fileAccess.CheckedAccessPath(newPath, out var isNewAccessed);
			ctx = new MoveDirectoryContext { FullOldPath = fullOldPath, FullNewPath = fullNewPath };

			if (!Directory.Exists(fullOldPath))
			{
				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FolderQuestion,
					StatusTitle = $"**{oldPath}**",
					ExpectedBehaviour = !isOldAccessed || !isNewAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = false,
					InterruptingContent = "Source directory not found."
				};
			}

			if (Directory.Exists(fullNewPath) && !overwrite)
			{
				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.PencilRemove,
					StatusTitle = $"**{oldPath}** → **{newPath}**",
					ExpectedBehaviour = !isOldAccessed || !isNewAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = false,
					InterruptingContent = "Destination directory already exists."
				};
			}

			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.Folder,
				StatusTitle = $"**{oldPath}** → **{newPath}**",
				ExpectedBehaviour = ToolBehaviour.DirectoryEdit |
				(!isOldAccessed || !isNewAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None)
			};
		}

		public ReactiveToolResult MoveDirectory(
			string oldPath,
			string newPath,
			bool overwrite = false,
			[SharedContext] MoveDirectoryContext? ctx = null)
		{
			try
			{
				var fullOldPath = ctx?.FullOldPath ?? _fileAccess.AccessPath(oldPath);
				var fullNewPath = ctx?.FullNewPath ?? _fileAccess.AccessPath(newPath);

				if (!Directory.Exists(fullOldPath))
				{
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.FolderQuestion,
						StatusTitle = $"**{oldPath}** → **{newPath}**",
						ResultContent = $"Source directory not found: {oldPath}"
					}.CompleteWithError();
				}

				if (Directory.Exists(fullNewPath) && !overwrite)
				{
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.PencilRemove,
						StatusTitle = $"**{oldPath}** → **{newPath}**",
						ResultContent = $"Destination directory already exists: {newPath}"
					}.CompleteWithError();
				}

				Directory.CreateDirectory(Path.GetDirectoryName(fullNewPath)!);
				Directory.Move(fullOldPath, fullNewPath);

				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.Folder,
					StatusTitle = $"**{oldPath}** → **{newPath}**",
					ResultContent = $"Directory moved from '{oldPath}' to '{newPath}'."
				}.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.PencilRemove,
					StatusTitle = $"**{oldPath}** → **{newPath}**",
					ResultContent = $"Error moving directory: {ex.Message}"
				}.CompleteWithError();
			}
		}

		public class CopyFileContext
		{
			public required string FullOldPath { get; init; }
			public required string FullNewPath { get; init; }
		}

		public PreviewToolExecutionResult PreviewCopyFile(
			string oldPath,
			string newPath,
			[SharedContext] out CopyFileContext? ctx,
			bool overwrite = false)
		{
			var fullOldPath = _fileAccess.CheckedAccessPath(oldPath, out var isOldAccessed);
			var fullNewPath = _fileAccess.CheckedAccessPath(newPath, out var isNewAccessed);
			ctx = new CopyFileContext { FullOldPath = fullOldPath, FullNewPath = fullNewPath };

			if (!File.Exists(fullOldPath))
			{
				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FileQuestion,
					StatusTitle = $"**{oldPath}**",
					ExpectedBehaviour = !isOldAccessed || !isNewAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = false,
					InterruptingContent = "Source file not found."
				};
			}

			if (File.Exists(fullNewPath) && !overwrite)
			{
				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.ContentCopy,
					StatusTitle = $"**{oldPath}** → **{newPath}**",
					ExpectedBehaviour = !isOldAccessed || !isNewAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = false,
					InterruptingContent = "Destination file already exists."
				};
			}

			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.ContentCopy,
				StatusTitle = $"**{oldPath}** → **{newPath}**",
				ExpectedBehaviour = ToolBehaviour.FileDirectoryCreate |
					(!isOldAccessed || !isNewAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None)
			};
		}

		public ReactiveToolResult CopyFile(
			string oldPath,
			string newPath,
			bool overwrite = false,
			[SharedContext] CopyFileContext? ctx = null)
		{
			try
			{
				var fullOldPath = ctx?.FullOldPath ?? _fileAccess.AccessPath(oldPath);
				var fullNewPath = ctx?.FullNewPath ?? _fileAccess.AccessPath(newPath);

				if (!File.Exists(fullOldPath))
				{
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.FileQuestion,
						StatusTitle = $"**{oldPath}** → **{newPath}**+",
						ResultContent = $"Source file not found: {oldPath}"
					}.CompleteWithError();
				}

				if (File.Exists(fullNewPath) && !overwrite)
				{
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.FileQuestion,
						StatusTitle = $"**{oldPath}** → **{newPath}**+",
						ResultContent = $"Destination file already exists: {newPath}"
					}.CompleteWithError();
				}

				if (Path.GetDirectoryName(fullNewPath) is string dir)
					Directory.CreateDirectory(dir);

				File.Copy(fullOldPath, fullNewPath, overwrite);

				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.ContentCopy,
					StatusTitle = $"**{oldPath}** → **{newPath}**+",
					ResultContent = $"File copied from '{oldPath}' to '{newPath}'."
				}.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.FileDocumentError,
					StatusTitle = $"**{oldPath}** → **{newPath}**+",
					ResultContent = $"Error copying file: {ex.Message}"
				}.CompleteWithError();
			}
		}

		public class CopyDirectoryContext
		{
			public required string FullOldPath { get; init; }
			public required string FullNewPath { get; init; }
		}

		public PreviewToolExecutionResult PreviewCopyDirectory(
			string oldPath,
			string newPath,
			[SharedContext] out CopyDirectoryContext? ctx,
			bool overwrite = false)
		{
			var fullOldPath = _fileAccess.CheckedAccessPath(oldPath, out var isOldAccessed);
			var fullNewPath = _fileAccess.CheckedAccessPath(newPath, out var isNewAccessed);
			ctx = new CopyDirectoryContext { FullOldPath = fullOldPath, FullNewPath = fullNewPath };

			if (!Directory.Exists(fullOldPath))
			{
				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.FolderQuestion,
					StatusTitle = $"**{oldPath}**",
					ExpectedBehaviour = !isOldAccessed || !isNewAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = false,
					InterruptingContent = "Source directory not found."
				};
			}

			if (Directory.Exists(fullNewPath) && !overwrite)
			{
				return new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.ContentCopy,
					StatusTitle = $"**{oldPath}** → **{newPath}**",
					ExpectedBehaviour = !isOldAccessed || !isNewAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None,
					InterruptingSuccess = false,
					InterruptingContent = "Destination directory already exists."
				};
			}

			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.ContentCopy,
				StatusTitle = $"**{oldPath}** → **{newPath}**",
				ExpectedBehaviour = ToolBehaviour.FileDirectoryCreate |
					(!isOldAccessed || !isNewAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None)
			};
		}

		public ReactiveToolResult CopyDirectory(
			string oldPath,
			string newPath,
			bool overwrite = false,
			[SharedContext] CopyDirectoryContext? ctx = null)
		{
			try
			{
				var fullOldPath = ctx?.FullOldPath ?? _fileAccess.AccessPath(oldPath);
				var fullNewPath = ctx?.FullNewPath ?? _fileAccess.AccessPath(newPath);

				if (!Directory.Exists(fullOldPath))
				{
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.FolderQuestion,
						StatusTitle = $"**{oldPath}** → **{newPath}**+",
						ResultContent = $"Source directory not found: {oldPath}"
					}.CompleteWithError();
				}

				if (Directory.Exists(fullNewPath) && !overwrite)
				{
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.FolderAlert,
						StatusTitle = $"**{oldPath}** → **{newPath}**+",
						ResultContent = $"Destination directory already exists: {newPath}"
					}.CompleteWithError();
				}

				Directory.CreateDirectory(fullNewPath);

				foreach (var file in Directory.GetFiles(fullOldPath, "*", SearchOption.AllDirectories))
				{
					var relativePath = Path.GetRelativePath(fullOldPath, file);
					var destFile = Path.Combine(fullNewPath, relativePath);
					Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
					File.Copy(file, destFile, overwrite);
				}

				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.ContentCopy,
					StatusTitle = $"**{oldPath}** → **{newPath}**+",
					ResultContent = $"Directory copied from '{oldPath}' to '{newPath}'."
				}.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.FolderAlert,
					StatusTitle = $"**{oldPath}** → **{newPath}**+",
					ResultContent = $"Error copying directory: {ex.Message}"
				}.CompleteWithError();
			}
		}
	}
}
