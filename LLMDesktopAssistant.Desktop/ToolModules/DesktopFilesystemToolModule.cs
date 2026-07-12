using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Tools;
using Material.Icons;

namespace LLMDesktopAssistant.Desktop.ToolModules
{
	[ToolModule]
	public class DesktopFilesystemToolModule : ToolModule
	{
		private readonly WorkingDirectoryAccessService _fileAccess;

		public DesktopFilesystemToolModule(WorkingDirectoryAccessService fileAccess)
		{
			_fileAccess = fileAccess;

			AddTool(OpenFile, OpenFileStreaming, OpenFilePreview,
				new ToolInitializationInfo
				{
					Name = "fs-open_file",
					Description = "Opens a file from the working directory with its default application.",
					Category = "filesystem",
					DefaultExpectedBehaviour = ToolBehaviour.ExecuteExternalProcess | ToolBehaviour.FileRead |
						ToolBehaviour.AccessOutsideWorkdir
				});
		}

		public StreamingToolArgumentsAnalysisResult OpenFileStreaming(
			string? path)
		{
			path ??= "?";
			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.OpenInNew,
				StatusTitle = $"**{path}**"
			};
		}

		public PreviewToolExecutionResult OpenFilePreview(
			string path, [SharedContext] out string fullPath)
		{
			fullPath = _fileAccess.CheckedAccessPath(path, out var isAccessed);

			if (!File.Exists(fullPath))
			{
				new PreviewToolExecutionResult
				{
					StatusIcon = MaterialIconKind.OpenInNew,
					StatusTitle = $"**{path}**",
					InterruptingSuccess = false,
					InterruptingContent = $"File not found: {path}"
				};
			}

			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.OpenInNew,
				StatusTitle = $"**{path}**",
				ExpectedBehaviour = ToolBehaviour.ExecuteExternalProcess | ToolBehaviour.FileRead |
					(!isAccessed ? ToolBehaviour.AccessOutsideWorkdir : 0)
			};
		}

		public ReactiveToolResult OpenFile(
			[SharedContext] string? fullPath,
			string path)
		{
			try
			{
				var workingDirectory = _fileAccess.GetWorkingDirectory();
				fullPath ??= _fileAccess.AccessPath(path);

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

				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.OpenInNew,
					StatusTitle = $"**{path}**",
					ResultContent = $"Successfully opened: {path}"
				}.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				return ReactiveToolResult.CreateError($"Error opening file {path}: {ex.Message}");
			}
		}

	}
}
