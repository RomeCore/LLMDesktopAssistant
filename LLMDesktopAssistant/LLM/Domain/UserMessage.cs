using System.Collections.Immutable;

namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents a user message in the chat session.
	/// </summary>
	public class UserMessage : ChatMessage
	{
		/// <summary>
		/// The collection of attachments associated with the user message. These can include images or files.
		/// </summary>
		public ImmutableList<Attachment> Attachments { get; init; } = [];

		private string? _llmProvidedContent;
		/// <summary>
		/// Gets or sets the content that will be provided to LLM and should not be displayed.
		/// Can include attachments, various notes, etc.
		/// </summary>
		public string? LLMProvidedContent
		{
			get => _llmProvidedContent;
			set => SetProperty(ref _llmProvidedContent, value);
		}
	}
}