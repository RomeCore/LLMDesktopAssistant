using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Services;
using RCLargeLanguageModels.Messages;
using Serilog;

namespace LLMDesktopAssistant.Prompting.Hooks;

/// <summary>
/// The message injector based on attaching <see cref="AttachedMessageAdditionalViewModel"/>.
/// </summary>
[Service(typeof(IPromptBuildingHook))]
public class AttachedMessageInjectionHook : IPromptBuildingHook
{
	public IEnumerable<IMessage>? ModifyFinalContext(IEnumerable<IMessage> messages, BranchedMessage message, ChatAgentDescriptor agent)
	{
		foreach (var attachedMessage in message.Message.AdditionalViewModels.OfType<AttachedMessageAdditionalViewModel>())
		{
			var attachedContent = attachedMessage.Content;
			if (string.IsNullOrWhiteSpace(attachedContent))
				continue;

			switch (attachedMessage.Mode)
			{
				default:
				case AttachedMessageMode.Prepend:
					messages = messages.Prepend(new RCLargeLanguageModels.Messages.UserMessage(attachedContent));
					break;

				case AttachedMessageMode.Append:
					messages = messages.Append(new RCLargeLanguageModels.Messages.UserMessage(attachedContent));
					break;

				case AttachedMessageMode.AgentPrivate:
					if (message.Message is LLM.Domain.AssistantMessage assistantMessage)
					{
						if (assistantMessage.SenderAgentId == agent.Id)
							messages = messages.Prepend(new RCLargeLanguageModels.Messages.UserMessage(attachedContent));
					}
					else
					{
						Log.Warning("Attached message mode AgentPrivate is only valid for assistant messages. Ignoring this message.");
					}
					break;
			}
		}
		return messages;
	}
}

