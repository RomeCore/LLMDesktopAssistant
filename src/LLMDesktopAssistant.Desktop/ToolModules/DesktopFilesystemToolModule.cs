using System.Diagnostics;
using System.IO;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Tools;
using Material.Icons;

namespace LLMDesktopAssistant.Desktop.ToolModules
{
	[ToolModule]
	public class DesktopFilesystemToolModule : ToolModule
	{
		private readonly WorkingDirectoryAccessService _fileAccess;
		private readonly IExplorerOpener _explorerOpener;

		public DesktopFilesystemToolModule(WorkingDirectoryAccessService fileAccess, IExplorerOpener explorerOpener)
		{
			_fileAccess = fileAccess;
			_explorerOpener = explorerOpener;

			AddTool(OpenFile, OpenFileStreaming, OpenFilePreview,
				new ToolInitializationInfo
				{
					Name = "fs-open_file",
					Description = "Opens a file from the working directory with its default application.",
					Category = "filesystem",
					DefaultExpectedBehaviour = ToolBehaviour.ExecuteExternalProcess | ToolBehaviour.FileRead |
						ToolBehaviour.AccessOutsideWorkdir
				});

			AddTool(OpenInExplorer, OpenInExplorerStreaming, OpenInExplorerPreview,
				new ToolInitializationInfo
				{
					Name = "fs-open_in_explorer",
					Description = "Opens a file or directory in the system file explorer. " +
						"Files are revealed and selected, directories are opened, and non-existent paths " +
						"fall back to opening their parent directory.",
					Category = "filesystem",
					DefaultExpectedBehaviour = ToolBehaviour.ExecuteExternalProcess | ToolBehaviour.AccessOutsideWorkdir
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
			fullPath = _fileAccess.CheckedAccessPath(path, DirectoryAccessMode.Execute, out var isAccessed);

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
				fullPath ??= _fileAccess.AccessPath(path, DirectoryAccessMode.Execute);

				if (!File.Exists(fullPath))
				{
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.OpenInNew,
						StatusTitle = $"**{path}**",
						ResultContent = $"File not found: {path}"
					}.CompleteWithError();
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
				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.OpenInNew,
					StatusTitle = $"**{path}**",
					ResultContent = $"Error opening file {path}: {ex.Message}"
				}.CompleteWithError();
			}
		}

		public StreamingToolArgumentsAnalysisResult OpenInExplorerStreaming(
			string? path)
		{
			path ??= "?";
			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.FolderOpen,
				StatusTitle = $"**{path}**"
			};
		}

		public PreviewToolExecutionResult OpenInExplorerPreview(
			string path, [SharedContext] out string fullPath)
		{
			fullPath = _fileAccess.CheckedAccessPath(path, DirectoryAccessMode.Execute, out var isAccessed);

			return new PreviewToolExecutionResult
			{
				StatusIcon = MaterialIconKind.FolderOpen,
				StatusTitle = $"**{path}**",
				ExpectedBehaviour = ToolBehaviour.ExecuteExternalProcess |
					(!isAccessed ? ToolBehaviour.AccessOutsideWorkdir : 0)
			};
		}

		public ReactiveToolResult OpenInExplorer(
			[SharedContext] string? fullPath,
			string path)
		{
			try
			{
				fullPath ??= _fileAccess.AccessPath(path, DirectoryAccessMode.Execute);

				if (!_explorerOpener.OpenPath(fullPath))
				{
					return new ReactiveToolResult
					{
						StatusIcon = MaterialIconKind.FolderOpen,
						StatusTitle = $"**{path}**",
						ResultContent = $"Failed to open in explorer: {path}"
					}.CompleteWithError();
				}

				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.FolderOpen,
					StatusTitle = $"**{path}**",
					ResultContent = $"Successfully opened in explorer: {path}"
				}.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.FolderOpen,
					StatusTitle = $"**{path}**",
					ResultContent = $"Error opening {path} in explorer: {ex.Message}"
				}.CompleteWithError();
			}
		}

	}
}
