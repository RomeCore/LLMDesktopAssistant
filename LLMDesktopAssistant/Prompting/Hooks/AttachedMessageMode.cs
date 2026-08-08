namespace LLMDesktopAssistant.Prompting.Hooks;

public enum AttachedMessageMode
{
	/// <summary>
	/// Prepend the attached message to the target message.
	/// </summary>
	Prepend,

	/// <summary>
	/// Append the attached message to the target message.
	/// </summary>
	Append,

	/// <summary>
	/// Prepend the target message to the attached message. Only visible to agent that have sent target message.
	/// </summary>
	AgentPrivate
}

