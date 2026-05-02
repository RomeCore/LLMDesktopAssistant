using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace LLMDesktopAssistant.Desktop.ToolModules
{
	[ToolModule]
	public class DesktopFilesystemToolModule : ToolModule
	{
		private readonly FileAccessService _fileAccess;

		public DesktopFilesystemToolModule(FileAccessService fileAccess)
		{
			_fileAccess = fileAccess;

			AddTool(OpenFile,
				new ToolInitializationInfo
				{
					Name = "fs-open_file",
					Description = "Opens a file from the working directory with its default application.",
					Category = "filesystem",
					AskForConfirmation = true
				});
		}

		public ReactiveToolResult OpenFile(string path)
		{
			try
			{
				var workingDirectory = _fileAccess.GetWorkingDirectory();
				var fullPath = _fileAccess.AccessPath(path);
				var fileName = Path.GetFileName(fullPath);

				if (!File.Exists(fullPath))
				{
					return ReactiveToolResult.CreateError($"File not found: {path}");
				}

				using (Process process = new Process())
				{
					process.StartInfo = new ProcessStartInfo
					{
						FileName = fullPath,
						WorkingDirectory = workingDirectory,
						UseShellExecute = true
					};
					process.Start();
				}

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.OpenInNew,
					StatusTitle = $"**{fileName}**",
					ResultContent = $"Successfully opened: {path}"
				};

				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error opening file {path}: {ex.Message}");
			}
		}

	}
}
