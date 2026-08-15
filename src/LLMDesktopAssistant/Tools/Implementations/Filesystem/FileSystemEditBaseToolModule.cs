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

			public required HunkGroups RejectedDiff { get; init; }

			public required string NewContent { get; init; }
		}

		protected async Task<DiffPostProcessResult> PostProcessDiffAsync(string filename, string oldContent, string newContent,
			ToolExecutionContext ctx, CancellationToken cancellationToken)
		{
			var diff = UnifiedDiff.Compute(oldContent, newContent, contextLines: 3);

			if (!ctx.RunningInUI)
				return new DiffPostProcessResult
				{
					Diff = diff,
					AppliedDiff = diff,
					RejectedDiff = new HunkGroups { Groups = [] },
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

				using var reg = cancellationToken.Register(() =>
				{
					diffVM.Decline();
				});

				var accepted = await diffVM.ConfirmationTask;
				if (accepted)
				{
					return new DiffPostProcessResult
					{
						Diff = diff,
						AppliedDiff = diffVM.BuildEnabledHunkGroups(),
						RejectedDiff = diffVM.BuildDisabledHunkGroups(),
						NewContent = diffVM.ApplyToText(oldContent)
					};
				}

				return new DiffPostProcessResult
				{
					Diff = diff,
					AppliedDiff = new HunkGroups { Groups = [] },
					RejectedDiff = diff,
					NewContent = diffVM.ApplyToText(oldContent)
				};
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
					RejectedDiff = new HunkGroups { Groups = [] },
					NewContent = newContent
				};
			}
		}
	}
}
