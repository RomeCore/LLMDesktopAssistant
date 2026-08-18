using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.SubAgents;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <summary>
	/// Provides access to the sub-agents available in the current session.
	/// </summary>
	public interface ISubAgentSetBuildingService
	{
		/// <summary>
		/// Returns all sub-agents available in the current session.
		/// </summary>
		/// <returns>A list of <see cref="SubAgentInfo"/> objects representing all sub-agents.</returns>
		IEnumerable<SubAgentInfo> GetAvailableSubAgents();

		/// <summary>
		/// Returns all sub-agents available for a specific agent in the current session.
		/// </summary>
		/// <param name="agent">The agent for which to retrieve sub-agents.</param>
		/// <returns>A list of <see cref="SubAgentInfo"/> objects representing all sub-agents available for the specified agent.</returns>
		IEnumerable<SubAgentInfo> GetSubAgentsForAgent(ChatAgentDescriptor agent);
	}
}
