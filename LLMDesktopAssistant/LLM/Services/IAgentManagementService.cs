using LLMDesktopAssistant.Agents;

namespace LLMDesktopAssistant.LLM.Services
{
	public interface IAgentManagementService
	{
		IEnumerable<Guid> ListAgents();
		AgentDescriptor GetAgentDescriptor(Guid agentId);
	}
}