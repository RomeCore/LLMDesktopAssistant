using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.Settings;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Prompting;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <inheritdoc cref="IAgentEffectiveMessagesProvider"/>
	[ChatService(typeof(IAgentEffectiveMessagesProvider))]
	public class AgentEffectiveMessagesProvider(
		Chat chat,
		IChatSettingsService chatSettings,
		IMessageVisibilityService messageVisibility) : IAgentEffectiveMessagesProvider
	{
		/// <inheritdoc/>
		public EffectiveChatContext GetEffectiveMessages(ChatAgentDescriptor agent)
		{
			var contextSettings = agent.Context;
			var promptMode = contextSettings.PromptMode;
			var disabledCheckpoints = contextSettings.GetEffectiveDisabledFlags(chatSettings.Settings);

			// In Hybrid mode the round window is not applied: anchors must always stay inside the effective set.
			int maxRounds = promptMode == PromptContextMode.Hybrid
				? 0
				: contextSettings.GetEffectiveMaxVisibleRounds(chatSettings.Settings);

			var messagesToProcess = MessagesInterface
				.GroupMessagesIntoRounds(chat.Messages, maxRounds)
				.SelectMany(g => g)
				.ToList();
			int effectiveMessagesStart = chat.Messages.Count - messagesToProcess.Count;

			var chatIndices = new Dictionary<BranchedMessage, int>(messagesToProcess.Count);
			for (int i = 0; i < messagesToProcess.Count; i++)
				chatIndices[messagesToProcess[i]] = i;

			List<BranchedMessage> result = [];
			List<(BranchedMessage Carrier, ContextCheckpoint Checkpoint)> candidates = [];

			bool hasSummary = false;
			bool encounteredUserMessage = false;

			for (int i = messagesToProcess.Count - 1; i >= 0; i--)
			{
				var branchedMessage = messagesToProcess[i];
				var message = branchedMessage.Message;

				// Checkpoints are immune to visibility checks and are processed first.
				// The checkpoint Kind is a mask: the agent-level disabled flags cut off the kinds it ignores.
				if (message.AdditionalData.TryGet<ContextCheckpoint>(out var checkpoint) && checkpoint.IsCompletedAndEnabled)
				{
					var activeKind = checkpoint.Kind & ~disabledCheckpoints;
					if (activeKind != ContextCheckpointKind.None)
					{
						candidates.Add((branchedMessage, checkpoint));

						if (activeKind.HasFlag(ContextCheckpointKind.Shield))
							break;

						if (activeKind.HasFlag(ContextCheckpointKind.Summary))
						{
							hasSummary = true;
							if (encounteredUserMessage)
								break;
						}
					}
				}

				if (message is UserMessage)
				{
					if (!messageVisibility.IsUserMessageVisibleToAgent(branchedMessage, agent))
						continue;
				}
				else if (message is AssistantMessage assistantMessage)
				{
					if (assistantMessage.IsCompleted && !messageVisibility.IsAssistantMessageVisibleToAgent(branchedMessage, agent))
						continue;
				}

				if (message is UserMessage || message is AssistantMessage { IsUserLike: true })
				{
					encounteredUserMessage = true;
					if (hasSummary)
					{
						result.Insert(0, branchedMessage);
						break;
					}
				}

				if (!hasSummary)
					result.Insert(0, branchedMessage);
			}

			var resultIndices = new Dictionary<BranchedMessage, int>(result.Count);
			for (int i = 0; i < result.Count; i++)
				resultIndices[result[i]] = i;

			var checkpoints = new List<EffectiveCheckpoint>(candidates.Count);
			int lastCutIndex = -1;
			int lastCheckpointIndex = -1;

			for (int i = candidates.Count - 1; i >= 0; i--)
			{
				var (carrier, candidate) = candidates[i];
				var kind = candidate.Kind & ~disabledCheckpoints;

				var index = GetRelativeIndex(carrier, result, resultIndices, chatIndices);
				
				if ((kind.HasFlag(ContextCheckpointKind.Shield) || kind.HasFlag(ContextCheckpointKind.Summary))
					&& index > lastCutIndex)
					lastCutIndex = index;

				if (index > lastCheckpointIndex)
					lastCheckpointIndex = index;

				checkpoints.Add(new EffectiveCheckpoint(candidate, index));
			}

			return new EffectiveChatContext(result, checkpoints, effectiveMessagesStart, lastCutIndex, lastCheckpointIndex);
		}

		/// <summary>
		/// Effective index of a carrier: its own index when it is in the effective set,
		/// otherwise the index of the nearest preceding visible message, otherwise -1.
		/// </summary>
		private static int GetRelativeIndex(BranchedMessage carrier, List<BranchedMessage> result,
			Dictionary<BranchedMessage, int> resultIndices, Dictionary<BranchedMessage, int> chatIndices)
		{
			if (resultIndices.TryGetValue(carrier, out int ownIndex))
				return ownIndex;

			int carrierPosition = chatIndices[carrier];
			for (int i = result.Count - 1; i >= 0; i--)
			{
				if (chatIndices[result[i]] < carrierPosition)
					return i;
			}

			return -1;
		}
	}
}
