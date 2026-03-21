using RCLargeLanguageModels.Messages;

namespace LLMDesktopAssistant.LLM.Data
{
	/// <summary>
	/// Represents a message wrapper for with conversation metadata (order and branch indices).
	/// </summary>
	public class ConversationMessage
	{
		/// <summary>
		/// The object that manages the conversation this message is associated with.
		/// </summary>
		public required ConversationManager Manager { get; init; }

		/// <summary>
		/// The message that being wrapped. Can be either user or assistant.
		/// </summary>
		public required ExtendedMessage Message { get; init; }

		/// <summary>
		/// The order of the message in the conversation.
		/// </summary>
		public int Order { get; init; } = -1;

		/// <summary>
		/// The number of branches that are available for this message's order index.
		/// </summary>
		public int AvailableBranches { get; init; } = 1;

		/// <summary>
		/// The index of the branch that is currently being used.
		/// </summary>
		public int BranchIndex { get; init; } = 0;
	}
}
