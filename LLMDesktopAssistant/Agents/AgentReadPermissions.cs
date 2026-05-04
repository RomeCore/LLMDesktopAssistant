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

		/// <summary>
		/// The agent can read attachments from other agents.
		/// Reserved for future use when agents can have attachments.
		/// </summary>
		OtherAgentAttachments = 1 << 6,

		/// <summary>
		/// The agent can read messages that contain tool calls from other agents.
		/// If not set, messages with tool calls may be filtered out even if OtherAgentMessages is set.
		/// </summary>
		MessagesWithToolCalls = 1 << 7,

		/// <summary>
		/// The agent can identify other agents as users.
		/// When set, agents that have <see cref="AgentExposureMode.IdentifySelfAsUser"/> will be
		/// visible and treated as user messages to this agent.
		/// </summary>
		IdentifyAgentsAsUsers = 1 << 8,
	}
}
