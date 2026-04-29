using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Settings;

namespace LLMDesktopAssistant.LLM.Services
{
	[ChatService(typeof(IAgentManagementService))]
	public class AgentManagementService(
		Chat chat
	) : IAgentManagementService
	{
		public IEnumerable<Guid> ListAgents()
		{
			var agents = SettingsManager.Get<AgentsConfiguration>().Agents;
			var chatAgents = chat.Settings.Agents.ChatAgents;
			return agents.Concat(chatAgents).Select(a => a.Id);
		}

		public AgentDescriptor GetAgentDescriptor(Guid agentId)
		{
			var agents = SettingsManager.Get<AgentsConfiguration>().Agents;
			var chatAgents = chat.Settings.Agents.ChatAgents;

			return agents.Concat(chatAgents).FirstOrDefault(a => a.Id == agentId) ?? 
				throw new KeyNotFoundException($"Agent with id '{agentId}' not found.");
		}
	}
}