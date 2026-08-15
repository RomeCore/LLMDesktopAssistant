namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents a branched message in the chat.
	/// </summary>
	public class BranchedMessage : Disposable
	{
		/// <summary>
		/// Gets the inner message that this <see cref="BranchedMessage"/> class wraps.
		/// </summary>
		public required ChatMessage Message { get; init; }

		/// <summary>
		/// Gets the ID of the message in the database. Will be -1 if not yet saved to the database.
		/// </summary>
		public required int MessageId { get; init; }

		/// <summary>
		/// Gets or sets the index of the message in the chat history (e.g. the index in the <see cref="Chat.Messages"/> collection).
		/// </summary>
		public required int MessageIndex { get; init; }

		/// <summary>
		/// Gets or sets the index of the selected branch.
		/// </summary>
		public int SelectedBranchIndex { get; init; } = 0;

		/// <summary>
		/// Gets or sets the count of available branches for current message.
		/// </summary>
		public int AvailableBranchesCount { get; init; } = 1;

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
				Message.Dispose();
		}

		/// <summary>
		/// Tries to get the inner message as a <see cref="UserMessage"/>. Throws an exception if it's not possible to do so.
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		public UserMessage AsUserMessage()
		{
			if (Message is UserMessage userMessage)
				return userMessage;
			throw new InvalidOperationException("The BranchedMessage does not contain a UserMessage.");
		}

		/// <summary>
		/// Tries to get the inner message as an <see cref="AssistantMessage"/>. Throws an exception if it's not possible to do so.
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		public AssistantMessage AsAssistantMessage()
		{
			if (Message is AssistantMessage assistantMessage)
				return assistantMessage;
			throw new InvalidOperationException("The BranchedMessage does not contain an AssistantMessage.");
		}

		public static implicit operator BranchedMessage(ChatMessage message)
		{
			return new BranchedMessage { Message = message, MessageId = -1, MessageIndex = -1 };
		}
	}
}