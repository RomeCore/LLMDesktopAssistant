namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents the status of a tool.
	/// </summary>
	public enum ToolStatus
	{
		/// <summary>
		/// The tool is just created and not yet initialized.
		/// </summary>
		None,

		/// <summary>
		/// The tool is currently pending for LLM to complete its arguments.
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
		Success,

		/// <summary>
		/// The tool has failed to complete.
		/// </summary>
		Error,

		/// <summary>
		/// The tool was cancelled by the user.
		/// </summary>
		Cancelled,

		/// <summary>
		/// The tool did not produce any result.
		/// </summary>
		NoResult
	}
}