namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Describes how this agent's messages are exposed to other agents.
	/// Controls what parts of an agent's output are visible to other agents when they read its messages.
	/// This is a per-agent setting that other agents' read permissions are checked against.
	/// Both the reader's <see cref="AgentReadPermissions"/> and the sender's <see cref="AgentExposureMode"/> must allow visibility.
	/// </summary>
	[Flags]
	public enum AgentExposureMode
	{
		/// <summary>
		/// Nothing is exposed. Other agents will not see any content from this agent.
		/// </summary>
		None = 0,

		/// <summary>
		/// Exposes the reasoning/thoughts of this agent to other agents.
		/// Other agents still need <see cref="AgentReadPermissions.OtherAgentReasoning"/> to read it.
		/// </summary>
		Reasoning = 1 << 0,

		/// <summary>
		/// Exposes the main content/text of this agent's messages to other agents.
		/// </summary>
		Content = 1 << 1,

		/// <summary>
		/// Exposes attachments (if any) of this agent's messages to other agents.
		/// Attachments for agents are not yet fully implemented but reserved for future use.
		/// </summary>
		Attachments = 1 << 2,

		/// <summary>
		/// Exposes tool calls and their results from this agent to other agents.
		/// Other agents still need <see cref="AgentReadPermissions.OtherAgentToolCalls"/> to read them.
		/// </summary>
		ToolCalls = 1 << 3,

		/// <summary>
		/// Exposes messages that contain tool calls.
		/// If this flag is not set, messages that include tool calls may be hidden entirely from other agents.
		/// </summary>
		MessagesWithToolCalls = 1 << 4,

		/// <summary>
		/// When set, this agent identifies itself as a user to other agents.
		/// Other agents will see messages from this agent as if they came from a user,
		/// meaning tool calls and reasoning become inaccessible regardless of other flags.
		/// Other agents still need <see cref="AgentReadPermissions.OtherAgentMessages"/> or
		/// <see cref="AgentReadPermissions.IdentifyAgentsAsUsers"/> to read messages from this agent.
		/// </summary>
		IdentifySelfAsUser = 1 << 5,
	}
}
