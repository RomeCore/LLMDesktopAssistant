using LLMDesktopAssistant.Agents.ExecutionStages;
using LLMDesktopAssistant.LLM.Domain;
using Serilog;

namespace LLMDesktopAssistant.LLM.Services
{
	[ChatService(typeof(IAgentOrderingService))]
	public class AgentOrderingService(
		Chat chat,
		IServiceProvider services
	) : IAgentOrderingService
	{
		public async Task<(Guid, Guid)?> GetNextAgentAsync(CancellationToken cancellationToken = default)
		{
			var stages = chat.Settings.Agents.ExecutionStages;
			if (stages.Count == 0)
				return null;

			var (roundUserMessages, roundAssistantMessages) = CollectThisRoundMessages();
			var executedInThisRound = CollectExecutedAgents(roundAssistantMessages);
			var (prevAgentId, prevStageId, prevAgentMessages) = GetPreviousExecutionInfo(roundAssistantMessages);
			var (currentStageExecuted, currentStageMessages) = GetCurrentStageInfo(roundAssistantMessages, prevStageId);

			int targetStageIndex = FindTargetStageIndex(prevStageId);

			if (targetStageIndex == -1)
			{
				currentStageMessages = [];
				currentStageExecuted = [];
				targetStageIndex = 0;
			}

			AgentPreExecutionContext BuildContext(
				IReadOnlyList<AssistantMessage> thisStageMessages,
				IReadOnlyList<Guid> executedInThisStage)
			{
				return new AgentPreExecutionContext
				{
					Services = services,
					ThisRoundUserMessages = roundUserMessages,
					ThisRoundAssistantMessages = roundAssistantMessages,
					ExecutedInThisRound = executedInThisRound,
					ThisStageMessages = thisStageMessages,
					ExecutedInThisStage = executedInThisStage,
					PrevousAgentMessages = prevAgentMessages,
					PrevousAgentExecuted = prevAgentId
				};
			}

			for (int i = targetStageIndex; i < stages.Count; i++)
			{
				var stage = stages[i];
				if (!stage.Enabled)
					continue;

				var context = BuildContext(currentStageMessages, currentStageExecuted);
				var nextAgentId = await stage.GetNextAgentAsync(context, cancellationToken);

				if (nextAgentId == null)
				{
					currentStageMessages = [];
					currentStageExecuted = [];
					continue;
				}

				return (nextAgentId.Value, stage.Id);
			}

			return null;
		}

		private (IReadOnlyList<UserMessage> UserMessages, IReadOnlyList<AssistantMessage> AssistantMessages) CollectThisRoundMessages()
		{
			var userMessages = new List<UserMessage>();
			var assistantMessages = new List<AssistantMessage>();

			bool foundUser = false;

			for (int i = chat.Messages.Count - 1; i >= 0; i--)
			{
				var message = chat.Messages[i].Message;

				if (message is AssistantMessage asm)
				{
					if (foundUser)
						break;
					assistantMessages.Add(asm);
				}
				else if (message is UserMessage usm)
				{
					userMessages.Add(usm);
					foundUser = true;
				}
				else
				{
					break;
				}
			}

			userMessages.Reverse();
			assistantMessages.Reverse();

			return (userMessages.AsReadOnly(), assistantMessages.AsReadOnly());
		}

		private IReadOnlyList<(Guid AgentId, Guid StageId)> CollectExecutedAgents(IReadOnlyList<AssistantMessage> assistantMessages)
		{
			var executedInThisRound = new List<(Guid AgentId, Guid StageId)>();

			Guid? prevAgentId = null, prevStageId = null;
			foreach (var msg in assistantMessages)
			{
				if (msg.SenderAgentId == prevAgentId && msg.AgentStageId == prevStageId)
					continue;

				executedInThisRound.Add((msg.SenderAgentId, msg.AgentStageId));

				prevAgentId = msg.SenderAgentId;
				prevStageId = msg.AgentStageId;
			}

			return executedInThisRound.AsReadOnly();
		}

		private (Guid? AgentId, Guid? StageId, IReadOnlyList<AssistantMessage> Messages) GetPreviousExecutionInfo(
			IReadOnlyList<AssistantMessage> thisRoundMessages)
		{
			if (thisRoundMessages.Count > 0 && thisRoundMessages[^1] is AssistantMessage asm)
			{
				var agentMessages = new List<AssistantMessage> { asm };

				for (int j = thisRoundMessages.Count - 2; j >= 0; j--)
				{
					if (thisRoundMessages[j] is AssistantMessage prevAsm
						&& prevAsm.SenderAgentId == asm.SenderAgentId)
					{
						agentMessages.Add(prevAsm);
					}
					else
					{
						break;
					}
				}

				agentMessages.Reverse();
				return (asm.SenderAgentId, asm.AgentStageId, agentMessages.AsReadOnly());
			}

			return (null, null, []);
		}

		private int FindTargetStageIndex(Guid? previousStageId)
		{
			if (previousStageId == null)
				return -1;

			var stages = chat.Settings.Agents.ExecutionStages;

			for (int i = 0; i < stages.Count; i++)
			{
				if (stages[i].Id == previousStageId && stages[i].Enabled)
					return i;
			}

			return -1;
		}

		private (IReadOnlyList<Guid> AgentIds, IReadOnlyList<AssistantMessage> Messages) GetCurrentStageInfo(
			IReadOnlyList<AssistantMessage> thisRoundMessages, Guid? stageId)
		{
			var agentIds = new List<Guid>();
			var messages = new List<AssistantMessage>();
			Guid? lastAgentId = null;

			for (int i = thisRoundMessages.Count - 1; i >= 0; i--)
			{
				var message = thisRoundMessages[i];
				if (message.AgentStageId != stageId)
					break;

				messages.Add(message);
				if (lastAgentId != message.SenderAgentId)
				{
					lastAgentId = message.SenderAgentId;
					agentIds.Add(message.SenderAgentId);
				}
			}

			agentIds.Reverse();
			messages.Reverse();
			return (agentIds.AsReadOnly(), messages.AsReadOnly());
		}
	}
}
