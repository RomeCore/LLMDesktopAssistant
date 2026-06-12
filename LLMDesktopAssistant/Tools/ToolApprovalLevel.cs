namespace LLMDesktopAssistant.Tools;

/// <summary>
/// Defines the approval policy for a tool — whether execution requires
/// user interaction, is automatically allowed, or follows agent-level
/// behaviour policies (<see cref="AgentToolSettings.AutoApproveBehaviours"/>
/// and <see cref="AgentToolSettings.DisallowBehaviours"/>).
/// </summary>
public enum ToolApprovalLevel
{
	/// <summary>
	/// Execute the tool immediately without any user interaction.
	/// This bypasses all agent-level policy checks.
	/// Use for trivially safe operations (math, random, time).
	/// </summary>
	AutoApprove = 0,

	/// <summary>
	/// Defer the decision to the agent's behaviour-based policy settings.
	/// The execution service will check <c>AutoApproveBehaviours</c> and
	/// <c>DisallowBehaviours</c> on the sending agent to determine whether
	/// to auto-approve, prompt, or reject the tool call.
	/// This is the recommended default for most tools.
	/// </summary>
	PolicyBased = 1,

	/// <summary>
	/// Always prompt the user for confirmation before executing the tool,
	/// regardless of the agent's <c>AutoApproveBehaviours</c>.
	/// Use for high-impact operations like file deletion or code execution.
	/// </summary>
	AlwaysAsk = 2,

	/// <summary>
	/// Always reject the tool call without executing it, regardless of the agent's policy settings.
	/// This explicitly notifies agents that they are not allowed to execute this tool upon trying to do so.
	/// </summary>
	AlwaysDisallow = 3,
}
