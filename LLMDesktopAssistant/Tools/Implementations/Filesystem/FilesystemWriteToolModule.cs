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
	public class FilesystemWriteToolModule : FileSystemEditBaseToolModule
	{
		private readonly WorkingDirectoryAccessService _fileAccess;

		public FilesystemWriteToolModule(WorkingDirectoryAccessService fileAccess)
		{
			_fileAccess = fileAccess;

			AddTool(WriteFile, WriteFileStream, WriteFilePreview,
				new ToolInitializationInfo
				{
					Name = "fs-write_file",
					Description = "Writes text content to a file inside working directory.",
					Category = "filesystem",
					DefaultExpectedBehaviour = ToolBehaviour.FileEdit | ToolBehaviour.FileDirectoryCreate,
					DefaultSelfHandledDecisions = ToolPolicyDecision.Approve | ToolPolicyDecision.Ask,
					SynchronizationGroup = FileSystemEditBaseToolModule.SyncGroup
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
			[SharedContext] out string? fullPath,
			string path,
			string content,
			bool append = false)
		{
			try
			{
				fullPath = _fileAccess.CheckedAccessPath(path, out var isAccessed);
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
								InterruptingContent = $"File **{path}** already contains the same content."
							};
						}

						return new PreviewToolExecutionResult
						{
							StatusIcon = Material.Icons.MaterialIconKind.FilePlus,
							StatusTitle = $"**{path}**",
							ExpectedBehaviour = ToolBehaviour.FileEdit |
								(!isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None)
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
					ExpectedBehaviour = ToolBehaviour.FileDirectoryCreate |
						(!isAccessed ? ToolBehaviour.AccessOutsideWorkdir : ToolBehaviour.None),
					SelfHandledDecisions = ToolPolicyDecision.None
				};
			}
			catch (Exception ex)
			{
				fullPath = null;
				return new PreviewToolExecutionResult
				{
					InterruptingContent = $"Error writing file: {ex.Message}",
					InterruptingSuccess = false
				};
			}
		}

		public async Task WriteFile(
			[SharedContext] string? fullPath,
			ReactiveToolResult result,
			ToolExecutionContext ctx,
			CancellationToken cancellationToken,
			string path,
			string content,
			bool append = false)
		{
			try
			{
				fullPath ??= _fileAccess.AccessPath(path);
				var dir = Path.GetDirectoryName(fullPath);

				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir!);

				var fileExisted = File.Exists(fullPath);
				string? oldContent = null;

				if (fileExisted)
					oldContent = File.ReadAllText(fullPath);
				if (append && fileExisted)
					content = oldContent + content;

				if (fileExisted)
				{
					var postProcessResult = await PostProcessDiffAsync(fullPath, oldContent!, content, ctx, cancellationToken);
					if (postProcessResult == null || !postProcessResult.AppliedDiff.HasGroups)
					{
						result.StatusIcon = Material.Icons.MaterialIconKind.FileDiscard;
						result.StatusTitle = $"**{path}**";
						result.ResultContent = "User has rejected the changes, none has applied.";
						result.CompleteWithSuccess();
						return;
					}

					File.WriteAllText(fullPath, postProcessResult.NewContent);

					var fileInfo = new FileInfo(fullPath);
					var size = FileUtils.BytesToDisplaySize(fileInfo.Length);

					var output = new StringBuilder();
					output.AppendLine($"File: {path}");
					output.AppendLine($"Operation: {(append ? "Append" : "Write")}");
					output.AppendLine($"New size: {fileInfo.Length} bytes ~ ({size})");

					// Compute and show diff for overwritten files
					string changesTitlePostfix = string.Empty;
					var diff = postProcessResult.AppliedDiff;
					if (diff.HasGroups)
					{
						var (removed, added) = diff.GetChangeCounts();

						output.AppendLine("[CHANGES]");
						output.AppendLine(diff.ToString());

						changesTitlePostfix = $" *(-{removed} +{added})*";
					}
					if (postProcessResult.RejectedDiff.HasGroups)
					{
						output.AppendLine("[REJECTED CHANGES BY THE USER]");
						output.AppendLine(postProcessResult.RejectedDiff.ToString());
					}
					if (!postProcessResult.Diff.HasGroups)
					{
						output.AppendLine("No changes has been applied.");
					}

					result.StatusIcon = append ? Material.Icons.MaterialIconKind.FileEdit : Material.Icons.MaterialIconKind.FileCheck;
					result.StatusTitle = $"**{path}**{changesTitlePostfix}";
					result.ResultContent = output.ToString();
					result.CompleteWithSuccess();
				}
				else
				{
					File.WriteAllText(fullPath, content);

					var fileInfo = new FileInfo(fullPath);
					var size = FileUtils.BytesToDisplaySize(fileInfo.Length);

					var output = new StringBuilder();
					output.AppendLine($"File: {path}");
					output.AppendLine($"Operation: Write");
					output.AppendLine($"Size: {fileInfo.Length} bytes ~ ({size})");

					result.StatusIcon = Material.Icons.MaterialIconKind.FilePlus;
					result.StatusTitle = $"**{path}**";
					result.ResultContent = output.ToString();
					result.CompleteWithSuccess();
				}

			}
			catch (Exception ex)
			{
				result.StatusIcon = Material.Icons.MaterialIconKind.FileAlert;
				result.StatusTitle = $"**{path}**";
				result.ResultContent = $"Error writing file: {ex.Message}";
				result.CompleteWithError();
			}
		}
	}
}
