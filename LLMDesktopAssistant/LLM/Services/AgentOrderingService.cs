using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.Services
{
	[ChatService(typeof(IAgentOrderingService))]
	public class AgentOrderingService(
		Chat chat,
		IAgentManagementService agentManager
	) : IAgentOrderingService
	{
		public async Task<Guid?> GetNextAgentAsync(CancellationToken cancellationToken = default)
		{
			var messages = chat.Messages;

			var activeAgents = chat.Settings.Agents.ActiveAgents
				.Where(a => a.Enabled)
				.Append(null);

			Guid? lastSenderId = (messages.LastOrDefault()?.Message as AssistantMessage)?.SenderAgent;
			Guid? prevAgent = null;
			bool foundPrevAgent = false;

			foreach (var activeAgent in activeAgents)
			{
				if (activeAgent == null)
					break;

				if (!foundPrevAgent)
				{
					foundPrevAgent = prevAgent == lastSenderId;
					prevAgent = activeAgent.AgentId;
				}
				if (foundPrevAgent)
				{
					var agent = agentManager.GetAgentDescriptor(activeAgent.AgentId);
					if (agent.ExecutionConditions.ExecutionChecker.ShouldExecute(chat, alreadyExecuted: false))
						return activeAgent.AgentId;
				}
			}

			return null;
		}
	}
}