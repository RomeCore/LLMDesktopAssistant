namespace LLMDesktopAssistant.LLM.Domain
{
	public enum MessageVisibility
	{
		/// <summary>
		/// The message is always visible to the all users and agents.
		/// </summary>
		Always = 0,

		/// <summary>
		/// The message is visible only after it has been sent to any of agents.
		/// </summary>
		RevealAfterSend,

		/// <summary>
		/// The message is visible only to users and not to agents.
		/// </summary>
		OnlyUsers,

		/// <summary>
		/// The message is visible only to agents and not to users.
		/// </summary>
		OnlyAgents
	}
}