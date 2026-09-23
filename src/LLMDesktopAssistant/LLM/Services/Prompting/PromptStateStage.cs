using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.Settings;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Prompting.State;
using Serilog;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <inheritdoc cref="IPromptStateStage"/>
	[ChatService(typeof(IPromptStateStage))]
	public class PromptStateStage(
		Chat chat,
		IEnumerable<IPromptSection> promptSections) : IPromptStateStage
	{
		/// <inheritdoc/>
		public PromptStateAnchorMessageData? Process(ChatAgentDescriptor agent, EffectiveChatContext effective,
			BranchedMessage? pendingResponse)
		{
			var promptMode = agent.Context.PromptMode;
			if (promptMode != PromptContextMode.Hybrid)
				return null;

			// Live anchor: the newest anchor of this agent positioned after the last cut.
			// A single message may hold anchors of multiple agents — scan all of them.
			for (int i = effective.Messages.Count - 1; i > effective.LastCutIndex; i--)
			{
				foreach (var candidate in effective.Messages[i].Message.AdditionalData.GetAll<PromptStateAnchorMessageData>())
				{
					if (candidate.AgentId != agent.Id)
						continue;

					Log.Debug("Reusing prompt state anchor #{AnchorId} for agent {AgentId} (message index {Index}).",
						candidate.Id, agent.Id, i);
					return candidate;
				}
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
			var states = promptSections.CaptureStates(agent);
			var snapshot = promptSections.RenderHeader(states);

			var anchor = new PromptStateAnchorMessageData
			{
				Id = GetNextAnchorId(),
				AgentId = agent.Id,
				Sections = [.. states],
				Snapshot = snapshot
			};
			anchor.IsVisible = false;

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
