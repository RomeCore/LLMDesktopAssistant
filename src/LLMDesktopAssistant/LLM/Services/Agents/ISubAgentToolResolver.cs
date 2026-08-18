using LLMDesktopAssistant.Agents.SubAgents;
using LLMDesktopAssistant.Agents.Tasks;

namespace LLMDesktopAssistant.LLM.Services.Agents
{
	/// <summary>
	/// Resolves the tools available to a sub-agent.
	/// </summary>
	public interface ISubAgentToolResolver
	{
		/// <summary>
		/// Resolves the tools of the specified sub-agent to executable <see cref="AgentTool"/> instances.
		/// </summary>
		/// <param name="agentInfo">The sub-agent information to resolve tools for.</param>
		/// <param name="errors">A list of errors encountered during the resolution process.</param>
		/// <returns>A list of <see cref="AgentTool"/> instances available to the sub-agent.</returns>
		IEnumerable<AgentTool> ResolveSubAgentTools(SubAgentInfo agentInfo, out List<string> errors);
	}
}
