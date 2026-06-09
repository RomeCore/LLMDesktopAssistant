using System.ComponentModel;
using System.Text;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Attachments;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils.Files;

namespace LLMDesktopAssistant.Tools.Implementations.Filesystem
{
	/// <summary>
	/// Tool module providing file write operations with integrated diff output.
	/// When overwriting an existing file, it computes and shows a line-based diff
	/// of the changes using a pure C# LCS-based algorithm (no git dependency).
	/// </summary>
	[ToolModule]
	public class FilesystemWriteToolModule : ToolModule
	{
		private readonly FileAccessService _fileAccess;

		public FilesystemWriteToolModule(FileAccessService fileAccess)
		{
			_fileAccess = fileAccess;

			AddTool(WriteFile,
				new ToolInitializationInfo
				{
					Name = "fs-write_file",
					Description = "Writes text content to a file inside working directory.",
					Category = "filesystem",
					AskForConfirmation = true
				});
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
				string? oldContent = null;

				// Capture old content before overwriting (for diff)
				if (!append && fileExisted)
				{
					try { oldContent = File.ReadAllText(fullPath); }
					catch { /* Best-effort: skip diff if unreadable */ }
				}

				if (append)
					File.AppendAllText(fullPath, content);
				else
					File.WriteAllText(fullPath, content);

				var fileInfo = new FileInfo(fullPath);
				var size = FileUtils.BytesToDisplaySize(fileInfo.Length);

				var output = new StringBuilder();
				output.AppendLine($"File: {path}");
				output.AppendLine($"Operation: {(append ? "Append" : fileExisted ? "Overwrite" : "Write")}");
				output.AppendLine($"New size: {fileInfo.Length} bytes ~ ({size})");

				// Compute and show diff for overwritten files
				if (!append && fileExisted && oldContent != null)
				{
					var diff = UnifiedDiff.Compute(oldContent, content);
					if (diff != null)
					{
						output.AppendLine();
						output.AppendLine("Changes:");
						output.AppendLine(diff);
					}
				}

				var result = new ReactiveToolResult
				{
					StatusIcon = fileExisted ?
						(append ? Material.Icons.MaterialIconKind.FileEdit : Material.Icons.MaterialIconKind.FileCheck) :
						Material.Icons.MaterialIconKind.FilePlus,
					StatusTitle = $"**{fileName}**",
					ResultContent = output.ToString()
				};

				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error writing file: {ex.Message}");
			}
		}
	}
}
