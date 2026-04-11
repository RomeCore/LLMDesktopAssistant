namespace LLMDesktopAssistant.Core.LLM.Data.Models
{
	/// <summary>
	/// Represents the status of a message.
	/// </summary>
	public enum MessageStatusModel
	{
		/// <summary>
		/// The message is pending and has not been processed yet.
		/// </summary>
		Pending,

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
}