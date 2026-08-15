using LLMDesktopAssistant.Agents.Tasks.MVVM;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Additional
{
	/// <summary>
	/// An <see cref="AdditionalMessageViewModel"/> that wraps an <see cref="AgentTaskViewModel"/>
	/// for inline display below an assistant message inside <see cref="ChatMessage.AdditionalViewModels"/>.
	/// </summary>
	public class AgentTaskListInlineViewModel : AdditionalMessageViewModel
	{
		/// <summary>
		/// The task view model to render inline.
		/// </summary>
		[ChangeTracker.Untracked]
		public required AgentTaskListViewModel TaskListViewModel { get; init; }

		/// <inheritdoc />
		public override int Order => 200;

		/// <summary>
		/// Initializes a new instance of the <see cref="AgentTaskInlineViewModel"/> class.
		/// </summary>
		public AgentTaskListInlineViewModel()
		{
			IsTemporary = true;
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing)
				TaskListViewModel.Dispose();

			base.Dispose(disposing);
		}
	}
}
