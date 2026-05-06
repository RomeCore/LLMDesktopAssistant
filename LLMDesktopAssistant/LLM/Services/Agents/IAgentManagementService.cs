using LLMDesktopAssistant.Agents;

namespace LLMDesktopAssistant.LLM.Services.Agents
{
	public interface IAgentManagementService
	{
		IEnumerable<Guid> ListAgentIds();
		IEnumerable<(AgentDescriptor Agent, bool IsGlobal)> ListAgents();
		AgentDescriptor GetAgentDescriptor(Guid agentId);
		AgentDescriptor? TryGetAgentDescriptor(Guid agentId);
	}
}