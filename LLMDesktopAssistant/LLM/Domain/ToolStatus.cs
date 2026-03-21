namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents the status of a tool.
	/// </summary>
	public enum ToolStatus
	{
		/// <summary>
		/// The tool is currently pending and has not yet started.
		/// </summary>
		Pending,

		/// <summary>
		/// The tool is currently waiting for user's approval before proceeding.
		/// </summary>
		WaitingForApproval,

		/// <summary>
		/// The tool is currently executing.
		/// </summary>
		Executing,

		/// <summary>
		/// The tool has completed successfully.
		/// </summary>
		Successful,

		/// <summary>
		/// The tool has failed to complete.
		/// </summary>
		Failed,

		/// <summary>
		/// The tool was cancelled by the user.
		/// </summary>
		CancelledByUser
	}
}