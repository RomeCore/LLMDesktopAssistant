using System.Text;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.Settings;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Prompting.Context;
using Serilog;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <inheritdoc cref="IPromptSectionProcessor"/>
	[ChatService(typeof(IPromptSectionProcessor))]
	public class PromptSectionProcessor(
		Chat chat) : IPromptSectionProcessor
	{
		/// <inheritdoc/>
		public PromptStateAnchorMessageData? Process(ChatAgentDescriptor agent,
			EffectiveChatContext effective, IEnumerable<IPromptContextProvider> providers)
		{
			var promptMode = agent.Context.PromptMode;
			if (promptMode != PromptContextMode.Hybrid)
				return null;

			var sections = providers.OfType<IPromptAnchoredSectionProvider>();

			PromptStateAnchorMessageData? anchor = null;
			int messageWithAnchor = -1;

			// Live anchor: the newest anchor of this agent positioned after the last cut.
			// A single message may hold anchors of multiple agents — scan all of them.
			for (int i = effective.Messages.Count - 1; i > effective.LastCutIndex; i--)
			{
				var message = effective.Messages[i];
				foreach (var candidate in message.Message.AdditionalData.GetAll<PromptStateAnchorMessageData>())
				{
					if (candidate.AgentId != agent.Id)
						continue;

					Log.Debug("Reusing prompt state anchor #{AnchorId} for agent {AgentId} (message index {Index}).",
						candidate.Id, agent.Id, i);
					anchor = candidate;
					messageWithAnchor = i;
					break;
				}
			}

			if (anchor is not null)
			{
				if (messageWithAnchor + 1 < effective.Messages.Count)
				{
					var deltasPerAnchor = new Dictionary<Type, List<PromptSectionDeltaBase>>();
					AssistantMessage? pendingAssistantMessage = null;

					for (int i = messageWithAnchor + 1; i < effective.Messages.Count; i++)
					{
						var message = effective.Messages[i];
						if (message.Message is AssistantMessage assistantMessage && assistantMessage.SenderAgentId == agent.Id)
						{
							foreach (var deltaData in message.Message.AdditionalData.OfType<PromptStateDeltaMessageData>())
							{
								if (deltaData.AnchorId != anchor.Id)
									continue;

								foreach (var delta in deltaData.Sections)
								{
									var deltaType = delta.GetType();
									
									if (!deltasPerAnchor.TryGetValue(deltaType, out var deltaList))
										deltasPerAnchor.Add(deltaType, deltaList = []);
									deltaList.Add(delta);
								}
							}

							if (i == effective.Messages.Count - 1 && !assistantMessage.IsCompleted)
								pendingAssistantMessage = assistantMessage;
						}
					}

					if (pendingAssistantMessage is null)
						throw new InvalidOperationException("Expected a pending assistant message, but none was found.");

					var deltas = new List<PromptSectionDeltaBase>();
					var sb = new StringBuilder();

					foreach (var section in sections)
					{
						var anchorState = anchor.Sections.FirstOrDefault(s => s.GetType() == section.StateType);
						var existingDeltas = deltasPerAnchor.GetValueOrDefault(section.DeltaType) ?? [];
						var actualState = section.CaptureState(agent);

						var newDelta = section.CalculateDelta(anchorState, existingDeltas, actualState, effective);
						if (newDelta is not null)
						{
							deltas.Add(newDelta);
							var rendered = section.RenderDelta(newDelta);
							sb.AppendLine(rendered);
						}
					}

					if (deltas.Count > 0)
					{
						while (sb.Length > 0 && char.IsWhiteSpace(sb[^1]))
							sb.Length--;

						pendingAssistantMessage.AdditionalData.Add(new PromptStateDeltaMessageData
						{
							AnchorId = anchor.Id,
							Sections = [.. deltas],
							Snapshot = sb.ToString()
						});
					}
				}

				return anchor;
			}

			// Rebaseline: create a new anchor on the first message after the last cut.
			int targetIndex = effective.LastCutIndex + 1;
			if (targetIndex >= effective.Messages.Count)
			{
				Log.Debug("Skipped prompt state anchor creation for agent {AgentId}: no target message after the last cut.",
					agent.Id);
				return null;
			}

			var target = effective.Messages[targetIndex];
			var states = sections.CaptureStates(agent);
			var snapshot = sections.RenderHeader(states);

			anchor = new PromptStateAnchorMessageData
			{
				Id = GetNextAnchorId(),
				AgentId = agent.Id,
				Sections = [.. states],
				Snapshot = snapshot
			};

			target.Message.AdditionalData.Add(anchor);
			Log.Information("Created prompt state anchor #{AnchorId} for agent {AgentId} on message {MessageId} (effective index {Index}).",
				anchor.Id, agent.Id, target.MessageId, targetIndex);
			return anchor;
		}

		private int GetNextAnchorId()
		{
			if (!chat.AdditionalData.TryGet<PromptStateAnchorIdCounter>(out var counter))
			{
				counter = new PromptStateAnchorIdCounter();
				chat.AdditionalData.TryReplace(counter);
			}

			counter.LastId++;
			return counter.LastId;
		}
	}
}
