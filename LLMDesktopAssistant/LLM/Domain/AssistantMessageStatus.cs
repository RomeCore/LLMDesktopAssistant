namespace LLMDesktopAssistant.LLM.Domain
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
		Successful,

		/// <summary>
		/// The message processing failed.
		/// </summary>
		Failed,

		/// <summary>
		/// The message was cancelled by the user.
		/// </summary>
		CancelledByUser
	}
}