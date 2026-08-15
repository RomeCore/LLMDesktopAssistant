using LLMDesktopAssistant.Agents;

namespace LLMDesktopAssistant.LLM.Services.Agents
{
	public interface IAgentManagementService
	{
		IEnumerable<Guid> ListAgentIds();
		IEnumerable<(ChatAgentDescriptor Agent, bool IsGlobal)> ListAgents();
		ChatAgentDescriptor GetAgentDescriptor(Guid agentId);
		ChatAgentDescriptor? TryGetAgentDescriptor(Guid agentId);
	}
}