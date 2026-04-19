namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents a branched message in the chat.
	/// </summary>
	public class BranchedMessage : Disposable
	{
		/// <summary>
		/// Gets or sets the message content.
		/// </summary>
		public required ChatMessage Message { get; init; }

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
	}
}