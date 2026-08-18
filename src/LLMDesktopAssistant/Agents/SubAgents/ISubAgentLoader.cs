namespace LLMDesktopAssistant.Agents.SubAgents
{
	/// <summary>
	/// Loads sub-agents from the provided file paths.
	/// </summary>
	public interface ISubAgentLoader
	{
		/// <summary>
		/// Loads sub-agents from the provided file paths.
		/// </summary>
		/// <param name="files">The sub-agent files to load.</param>
		/// <returns>A list of loaded sub-agents.</returns>
		IEnumerable<SubAgentInfo> Load(IEnumerable<SubAgentFileInfo> files);
	}
}
