namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents the current execution status of a chat session.
	/// </summary>
	public enum ChatStatus
	{
		/// <summary>
		/// The chat is idle and no operations are being performed.
		/// </summary>
		Idle,

		/// <summary>
		/// The chat is executing a response generation or an agent task.
		/// </summary>
		Executing,

		/// <summary>
		/// The chat is waiting for a user confirmation (e.g. tool approval).
		/// </summary>
		Confirming
	}
}
