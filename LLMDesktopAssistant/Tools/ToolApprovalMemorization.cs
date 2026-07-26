namespace LLMDesktopAssistant.Tools;

/// <summary>
/// Enumerates the options for memorizing tool approval.
/// </summary>
public enum ToolApprovalMemorization
{
	/// <summary>
	/// Approve/deny the tool only once.
	/// </summary>
	Once,

	/// <summary>
	/// Memorize the approval for the duration of the current chat session.
	/// </summary>
	Session,

	/// <summary>
	/// Memorize the approval for the current agentic task.
	/// </summary>
	Task,

	/// <summary>
	/// Approve/deny the tool every time it is used. Overrides the settings.
	/// </summary>
	Always
}
