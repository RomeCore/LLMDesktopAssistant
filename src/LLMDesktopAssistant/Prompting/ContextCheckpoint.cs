using LLMDesktopAssistant.LLM.MVVM.Additional;

namespace LLMDesktopAssistant.Prompting
{
	public class ContextCheckpoint : AdditionalChatData
	{
		/// <summary>
		/// Gets or sets the kind of context checkpoint this is.
		/// </summary>
		public ContextCheckpointKind Kind
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// Gets or sets the textual context associated with this checkpoint.
		/// This can be a summary if the kind is 'summary'.
		/// </summary>
		public string? Context
		{
			get;
			set => SetProperty(ref field, value);
		}
	}
}
