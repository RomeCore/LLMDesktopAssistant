using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.ToolModules;
using RCLargeLanguageModels.Tools;
using System.Collections.Specialized;
using System.IO;

namespace LLMDesktopAssistant.LLM.Services.Tools
{
	public class FilesystemToolModule : ToolModule
	{
		private readonly Chat _chat;

		public FilesystemToolModule(Chat chat)
		{
			_chat = chat;

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(ReadFile, "fs-read_file",
					"Reads text file content from the working directory."),
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
		}

		private string ResolvePath(string path)
		{
			if (string.IsNullOrWhiteSpace(_chat.Settings.WorkingDirectory))
				throw new InvalidOperationException("Working directory is not set.");

			var baseDir = Path.GetFullPath(_chat.Settings.WorkingDirectory);
			var fullPath = Path.GetFullPath(Path.Combine(baseDir, path));

			if (!fullPath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
				throw new AccessViolationException("Access outside working directory is not allowed.");

			return fullPath;
		}

		public override IEnumerable<ToolInfo> GetTools()
		{
			if (!string.IsNullOrWhiteSpace(_chat.Settings.WorkingDirectory) && Directory.Exists(_chat.Settings.WorkingDirectory))
				return base.GetTools();
			return [];
		}

		public ToolResult ReadFile(string path)
		{
			try
			{
				var fullPath = ResolvePath(path);

				if (!File.Exists(fullPath))
					return new ToolResult(ToolResultStatus.Error, "File not found.");

				var content = File.ReadAllText(fullPath);

				return new ToolResult(ToolResultStatus.Success, content);
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error reading file: {ex.Message}");
			}
		}

		public ToolResult WriteFile(string path, string content, bool append = false)
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

		public ToolResult ListDirectory(string path = "")
		{
			try
			{
				var fullPath = ResolvePath(path);

				if (!Directory.Exists(fullPath))
					return new ToolResult(ToolResultStatus.Error, "Directory not found.");

				var entries = Directory.GetFileSystemEntries(fullPath)
					.Select(e =>
					{
						var name = Path.GetFileName(e);
						var isDirectory = Directory.Exists(e);

						if (isDirectory)
						{
							var editedAt = Directory.GetLastWriteTime(e).ToString("yyyy-MM-dd HH:mm:ss");
							var countOfEntries = Directory.GetFileSystemEntries(e).Length;
							return $"[DIR] {name} ({countOfEntries} entries, edited at {editedAt})";
						}
						else // File
						{
							var editedAt = File.GetLastWriteTime(e).ToString("yyyy-MM-dd HH:mm:ss");
							var size = new FileInfo(e).Length;
							var sizeStr = $"{size} B";

							if (size > 10240)
							{
								size /= 1024;
								sizeStr = $"{size} KB";

								if (size > 10240)
								{
									size /= 1024;
									sizeStr = $"{size} MB";

									if (size > 10240)
									{
										size /= 1024;
										sizeStr = $"{size} GB";
									}
								}
							}

							return $"[FILE] {name} ({sizeStr}, edited at {editedAt})";
						}
					});

				return new ToolResult(ToolResultStatus.Success, string.Join(Environment.NewLine, entries));
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
	}
}