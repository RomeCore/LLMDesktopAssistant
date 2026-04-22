using LLMDesktopAssistant.LLM.Domain;
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
		private readonly Chat _chat;

		public DesktopFilesystemToolModule(Chat chat)
		{
			_chat = chat;

			AddTool(OpenFile,
				new ToolInitializationInfo
				{
					Name = "fs-open_file",
					Description = "Opens a file from the working directory with its default application.",
					Category = "filesystem",
					AskForConfirmation = true
				});
		}

		public ReactiveToolResult OpenFile(string filename)
		{
			try
			{
				var workDir = _chat.Settings.GetWorkingDirectory();
				var fullPath = Path.Combine(workDir, filename);
				var fileName = Path.GetFileName(fullPath);

				if (!File.Exists(fullPath))
				{
					return ReactiveToolResult.CreateError($"File not found: {filename}");
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

				var result = new ReactiveToolResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.OpenInNew,
					StatusTitle = $"**{fileName}**",
					ResultContent = $"Successfully opened: {filename}"
				};

				return result.Complete(true);
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error opening file {filename}: {ex.Message}");
			}
		}

	}
}
