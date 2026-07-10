using LLMDesktopAssistant.Tools.MVVM.Diff;
using LLMDesktopAssistant.Utils.Files;

namespace LLMDesktopAssistant.Tools.Implementations.Filesystem
{
	public abstract class FileSystemEditBaseToolModule : ToolModule
	{
		protected const string SyncGroup = "fs-edit";

		protected class DiffPostProcessResult
		{
			public required HunkGroups Diff { get; init; }

			public required HunkGroups AppliedDiff { get; init; }

			public required string NewContent { get; init; }
		}

		protected async Task<DiffPostProcessResult?> PostProcessDiffAsync(string filename, string oldContent, string newContent, ToolExecutionContext ctx)
		{
			var diff = UnifiedDiff.Compute(oldContent, newContent, contextLines: 3);

			if (!ctx.RunningInUI)
				return new DiffPostProcessResult
				{
					Diff = diff,
					AppliedDiff = diff,
					NewContent = newContent
				};

			if (ctx.PolicyDecision == ToolPolicyDecision.Ask)
			{
				var diffVM = new TextDiffAdditionalViewModel
				{
					Title = filename,
					IsReadOnly = false
				};
				diffVM.LoadFromHunkGroups(diff);
				ctx.Message.AdditionalViewModels.Add(diffVM);

				var accepted = await diffVM.ConfirmationTask;
				if (accepted)
				{
					return new DiffPostProcessResult
					{
						Diff = diff,
						AppliedDiff = diffVM.BuildHunkGroups(),
						NewContent = diffVM.ApplyToText(oldContent)
					};
				}

				return null;
			}
			else
			{
				var diffVM = new TextDiffAdditionalViewModel
				{
					IsReadOnly = true
				};
				diffVM.LoadFromHunkGroups(diff);
				ctx.Message.AdditionalViewModels.Add(diffVM);
				return new DiffPostProcessResult
				{
					Diff = diff,
					AppliedDiff = diff,
					NewContent = newContent
				};
			}
		}
	}
}
