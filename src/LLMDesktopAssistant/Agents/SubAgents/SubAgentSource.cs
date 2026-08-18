namespace LLMDesktopAssistant.Agents.SubAgents
{
	/// <summary>
	/// Specifies the source of a sub-agent.
	/// </summary>
	public enum SubAgentSource
	{
		/// <summary>
		/// The source of the sub-agent is unknown.
		/// </summary>
		Unknown,

		/// <summary>
		/// The sub-agent is defined in .llt template files.
		/// </summary>
		Template,

		/// <summary>
		/// The sub-agent is defined in the %LOCALAPPDATA%/.llmassist/agents/ or ~/*agent_home*/agents/ directories.
		/// </summary>
		UserProfile,

		/// <summary>
		/// The sub-agent is defined in the ./*agent_home*/agents/ directories.
		/// </summary>
		WorkingDirectory,

		/// <summary>
		/// The sub-agent is defined in a custom location.
		/// </summary>
		Custom
	}
}
