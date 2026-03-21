using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents a chat session.
	/// </summary>
	public class Chat
	{
		/// <summary>
		/// The collection of messages in the chat session.
		/// </summary>
		public RangeObservableCollection<BranchedMessage> Messages { get; } = [];

		/// <summary>
		/// Gets or sets the system prompt for the chat session. Used to provide context and instructions to the model.
		/// </summary>
		public string? SystemPrompt { get; set; }

		/// <summary>
		/// The list of properties associated with the chat session. These can include additional settings or configurations that affect the behavior of the chat session.
		/// </summary>
		public List<ChatProperty> Properties { get; } = [];
	}
}