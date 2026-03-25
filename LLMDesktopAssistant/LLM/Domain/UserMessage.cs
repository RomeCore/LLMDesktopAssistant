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
		public List<Attachment> Attachments { get; set; } = [];
	}
}