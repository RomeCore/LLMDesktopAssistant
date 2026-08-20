using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.LLM.Settings;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <summary>
	/// Determines whether chat messages are visible to a given agent, respecting the agent's
	/// read permissions, the sender agent's exposure mode and the message visibility settings.
	/// </summary>
	public static class AgentMessageVisibility
	{
		/// <summary>
		/// Determines whether the specified user message is visible to the given agent.
		/// </summary>
		/// <param name="message">The branched user message to check.</param>
		/// <param name="agent">The agent to check visibility for.</param>
		/// <param name="chatSettings">The chat settings used for read permission resolution.</param>
		/// <returns><see langword="true"/> if the message is visible to the agent; otherwise, <see langword="false"/>.</returns>
		public static bool IsUserMessageVisibleToAgent(BranchedMessage message, ChatAgentDescriptor agent, ChatSettings chatSettings)
		{
			var userMessage = message.AsUserMessage();
			var permissions = agent.Read.GetEffectiveReadPermissions(chatSettings);

			if (!permissions.HasFlag(AgentReadPermissions.UserMessages))
				return false;

			switch (userMessage.Visibility)
			{
				case Domain.MessageVisibility.OnlyUsers:
					return false;
				case Domain.MessageVisibility.OnlyAgents:
				case Domain.MessageVisibility.Always:
				case Domain.MessageVisibility.RevealAfterSend:
				default:
					break;
			}

			// If its a white list, then 'contains' must return true to skip this check -> true == true
			// If its a black list, then 'contains' must return false to skip this check -> false == false
			if (userMessage.VisibleTo.Contains(agent.Id.ToString()) != userMessage.IsVisibleToWhiteList)
				return false;

			return true;
		}

		/// <summary>
		/// Determines whether the specified assistant message is visible to the given agent.
		/// </summary>
		/// <param name="message">The branched assistant message to check.</param>
		/// <param name="agent">The agent to check visibility for.</param>
		/// <param name="agentManager">The agent management service used to resolve the sender agent descriptor.</param>
		/// <param name="chatSettings">The chat settings used for permission and exposure resolution.</param>
		/// <returns><see langword="true"/> if the message is visible to the agent; otherwise, <see langword="false"/>.</returns>
		public static bool IsAssistantMessageVisibleToAgent(BranchedMessage message, ChatAgentDescriptor agent,
			IAgentManagementService agentManager, ChatSettings chatSettings)
		{
			var assistantMessage = message.AsAssistantMessage();
			var messageAgentId = assistantMessage.SenderAgentId;
			var agentDescriptor = agentManager.GetAgentDescriptor(assistantMessage.SenderAgentId);
			var exposure = agentDescriptor.Read.GetEffectiveExposureMode(chatSettings); // What sender agent exposes
			var permissions = agent.Read.GetEffectiveReadPermissions(chatSettings); // What current agent can see

			// Own messages
			if (messageAgentId == agent.Id)
				return permissions.HasFlag(AgentReadPermissions.OwnMessages);

			// User-like messages are treated as user messages: gated by user read permissions,
			// tool calls and reasoning are inaccessible regardless of other flags.
			if (assistantMessage.IsUserLike)
				return permissions.HasFlag(AgentReadPermissions.UserMessages);

			// Other agent messages
			if (!permissions.HasFlag(AgentReadPermissions.OtherAgentMessages))
				return false;

			// Messages with tool calls
			if (assistantMessage.ToolCalls.Count > 0 && !(permissions.HasFlag(AgentReadPermissions.MessagesWithToolCalls)
				&& exposure.HasFlag(AgentExposureMode.MessagesWithToolCalls)))
				return false;

			// Apply agent ID filter (white/black list)
			var filter = agent.Read.AgentIdsReadFilter;
			if (filter.Count > 0)
			{
				bool inFilter = filter.Contains(messageAgentId);
				if (agent.Read.IsFilterWhiteList && !inFilter)
					return false;
				if (!agent.Read.IsFilterWhiteList && inFilter)
					return false;
			}

			return true;
		}
	}
}
