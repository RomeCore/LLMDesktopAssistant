namespace LLMDesktopAssistant.Core.LLM.Domain
{
	/// <summary>
	/// Represents the status of an assistant message.
	/// </summary>
	public enum AssistantMessageStatus
	{
		/// <summary>
		/// The message is pending and has not been processed yet.
		/// </summary>
		Pending,

		/// <summary>
		/// The message is being streamed and will be updated as it progresses.
		/// </summary>
		Streaming,

		/// <summary>
		/// The message has been successfully processed.
		/// </summary>
		Success,

		/// <summary>
		/// The message processing failed.
		/// </summary>
		Error,

		/// <summary>
		/// The message was cancelled by the user.
		/// </summary>
		Cancelled
	}

	public static class AssistantMessageStatusExtensions
	{
		/// <summary>
		/// Determines if the given message status is a terminal state, meaning it is finished.
		/// </summary>
		/// <param name="status">The message status to check.</param>
		/// <returns>True if the status is terminal, otherwise false.</returns>
		public static bool IsTerminal(this AssistantMessageStatus status)
		{
			return status == AssistantMessageStatus.Success || status == AssistantMessageStatus.Error || status == AssistantMessageStatus.Cancelled;
		}
	}
}