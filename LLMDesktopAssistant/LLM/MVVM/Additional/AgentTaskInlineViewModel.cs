using LLMDesktopAssistant.Agents.Tasks.MVVM;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Additional
{
	/// <summary>
	/// An <see cref="AdditionalMessageViewModel"/> that wraps an <see cref="AgentTaskViewModel"/>
	/// for inline display below an assistant message inside <see cref="ChatMessage.AdditionalViewModels"/>.
	/// </summary>
	public class AgentTaskInlineViewModel : AdditionalMessageViewModel
	{
		/// <summary>
		/// The task view model to render inline.
		/// </summary>
		[ChangeTracker.Untracked]
		public required AgentTaskViewModel TaskViewModel { get; init; }

		/// <inheritdoc />
		public override int Order => 200;

		/// <summary>
		/// Initializes a new instance of the <see cref="AgentTaskInlineViewModel"/> class.
		/// </summary>
		public AgentTaskInlineViewModel()
		{
			IsTemporary = true;
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing)
				TaskViewModel.Dispose();

			base.Dispose(disposing);
		}
	}
}
