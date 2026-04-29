namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Describes the mode in which an agent can read messages.
	/// </summary>
	[Flags]
	public enum AgentReadPermissions
	{
		/// <summary>
		/// The agent cannot read anything. This is invalid mode.
		/// </summary>
		None = 0,

		/// <summary>
		/// The agent can read user messages.
		/// </summary>
		UserMessages = 1 << 0,

		/// <summary>
		/// The agent can read user attachments.
		/// </summary>
		UserAttachments = 1 << 1,

		/// <summary>
		/// The agent can read its own messages.
		/// </summary>
		OwnMessages = 1 << 2,

		/// <summary>
		/// The agent can read messages from other agents.
		/// </summary>
		OtherAgentMessages = 1 << 3,

		/// <summary>
		/// The agent can read the reasoning of other agents.
		/// </summary>
		OtherAgentReasoning = 1 << 4,

		/// <summary>
		/// The agent can read the tool calls of other agents.
		/// </summary>
		OtherAgentToolCalls = 1 << 5,
	}
}