using System.Text;
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

			AddTool(WriteFile, WriteFileStream, WriteFilePreview,
				new ToolInitializationInfo
				{
					Name = "fs-write_file",
					Description = "Writes text content to a file inside working directory.",
					Category = "filesystem",
					DefaultExpectedBehaviour = ToolBehaviour.FileEdit | ToolBehaviour.FileDirectoryCreate
				});
		}

		public StreamingToolArgumentsAnalysisResult? WriteFileStream(
			string? path,
			string? content,
			bool append = false)
		{
			int lines = 0;
			if (content != null)
				foreach (var line in content.EnumerateLines())
					lines++;

			return new StreamingToolArgumentsAnalysisResult
			{
				StatusTitle = LocalizationManager.LocalizeStaticFormat("fs-write_file_streaming_status",
					path != null ? $"**{path}**" : string.Empty,
					lines)
			};
		}

		public PreviewToolExecutionResult? WriteFilePreview(
			string path,
			string content,
			bool append = false)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);
				var fileExisted = File.Exists(fullPath);

				if (fileExisted)
				{
					try
					{
						var oldContent = File.ReadAllText(fullPath);
						if (oldContent == content)
						{
							return new PreviewToolExecutionResult
							{
								StatusIcon = Material.Icons.MaterialIconKind.FileQuestion,
								StatusTitle = LocalizationManager.LocalizeStaticFormat("fs-edit_changes_applied_none", $"**{path}**"),
								InterruptingSuccess = true,
								InterruptingContent = $"File **{path}** already contains the same content.",
								ExpectedBehaviour = ToolBehaviour.None
							};
						}

						var diff = UnifiedDiff.Compute(oldContent, content, contextLines: 0);
						int removed = 0, added = 0;
						foreach (var group in diff)
						{
							if (group.OldCount != -1)
								removed += group.OldCount;
							if (group.NewCount != -1)
								added += group.NewCount;
						}
						return new PreviewToolExecutionResult
						{
							StatusIcon = Material.Icons.MaterialIconKind.FilePlus,
							StatusTitle = $"**{path}** *(-{removed} +{added})*",
							ExpectedBehaviour = ToolBehaviour.FileEdit
						};
					}
					catch
					{
						return null;
					}
				}

				return new PreviewToolExecutionResult
				{
					StatusIcon = Material.Icons.MaterialIconKind.FilePlus,
					StatusTitle = $"**{path}**",
					ExpectedBehaviour = ToolBehaviour.FileDirectoryCreate
				};
			}
			catch (Exception ex)
			{
				return new PreviewToolExecutionResult
				{
					InterruptingContent = $"Error writing file: {ex.Message}",
					InterruptingSuccess = false
				};
			}
		}

		public ReactiveToolResult WriteFile(
			string path,
			string content,
			bool append = false)
		{
			try
			{
				var fullPath = _fileAccess.AccessPath(path);
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
				string changesTitlePostfix = string.Empty;
				if (!append && fileExisted && oldContent != null)
				{
					var diff = UnifiedDiff.Compute(oldContent, content);
					if (diff.HasGroups)
					{
						output.AppendLine();
						output.AppendLine("Changes:");
						output.AppendLine(diff.ToString());

						var (removed, added) = diff.GetChangeCounts();
						changesTitlePostfix = $" *(-{removed} +{added})*";
					}
				}

				var result = new ReactiveToolResult
				{
					StatusIcon = fileExisted ?
						(append ? Material.Icons.MaterialIconKind.FileEdit : Material.Icons.MaterialIconKind.FileCheck) :
						Material.Icons.MaterialIconKind.FilePlus,
					StatusTitle = $"**{path}**{changesTitlePostfix}",
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
