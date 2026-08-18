using LLMDesktopAssistant.Agents.SubAgents;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <summary>
	/// Locates sub-agent files based on current configuration.
	/// </summary>
	public interface ISubAgentLocator
	{
		/// <summary>
		/// Locates all sub-agent files based on current configuration.
		/// </summary>
		/// <returns>A list of sub-agent file paths.</returns>
		IEnumerable<SubAgentFileInfo> LocateSubAgentFiles();
	}
}
