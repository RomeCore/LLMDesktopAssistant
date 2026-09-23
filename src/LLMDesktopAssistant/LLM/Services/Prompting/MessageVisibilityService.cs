using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.Settings;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.LLM.Settings;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <inheritdoc cref="IMessageVisibilityService"/>
	[ChatService(typeof(IMessageVisibilityService))]
	public class MessageVisibilityService(
		IChatSettingsService chatSettings,
		IAgentManagementService agentManager) : IMessageVisibilityService
	{
		/// <inheritdoc/>
		public bool IsUserMessageVisibleToAgent(BranchedMessage message, ChatAgentDescriptor agent)
		{
			var userMessage = message.AsUserMessage();
			var permissions = agent.Read.GetEffectiveReadPermissions(chatSettings.Settings);

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

		/// <inheritdoc/>
		public bool IsAssistantMessageVisibleToAgent(BranchedMessage message, ChatAgentDescriptor agent)
		{
			var assistantMessage = message.AsAssistantMessage();
			var messageAgentId = assistantMessage.SenderAgentId;
			var agentDescriptor = agentManager.GetAgentDescriptor(assistantMessage.SenderAgentId);
			var exposure = agentDescriptor.Read.GetEffectiveExposureMode(chatSettings.Settings); // What sender agent exposes
			var permissions = agent.Read.GetEffectiveReadPermissions(chatSettings.Settings); // What current agent can see

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
