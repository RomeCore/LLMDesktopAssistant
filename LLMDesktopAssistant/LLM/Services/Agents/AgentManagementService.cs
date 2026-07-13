using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Settings;

namespace LLMDesktopAssistant.LLM.Services.Agents
{
	[ChatService(typeof(IAgentManagementService))]
	public class AgentManagementService(
		Chat chat
	) : IAgentManagementService
	{
		public IEnumerable<Guid> ListAgentIds()
		{
			var agents = SettingsManager.Get<AgentsConfiguration>().Agents;
			var chatAgents = chat.Settings.Agents.ChatAgents;
			return chatAgents.Concat(agents).Select(a => a.Id);
		}

		public IEnumerable<(ChatAgentDescriptor Agent, bool IsGlobal)> ListAgents()
		{
			var agents = SettingsManager.Get<AgentsConfiguration>().Agents;
			var chatAgents = chat.Settings.Agents.ChatAgents;
			return chatAgents.Select(a => (a, false)).Concat(agents.Select(a => (a, true)));
		}

		public ChatAgentDescriptor GetAgentDescriptor(Guid agentId)
		{
			return TryGetAgentDescriptor(agentId) ?? 
				throw new KeyNotFoundException($"Agent with id '{agentId}' not found.");
		}

		public ChatAgentDescriptor? TryGetAgentDescriptor(Guid agentId)
		{
			var agents = SettingsManager.Get<AgentsConfiguration>().Agents;
			var chatAgents = chat.Settings.Agents.ChatAgents;

			return chatAgents.Concat(agents).FirstOrDefault(a => a.Id == agentId);
		}
	}
}