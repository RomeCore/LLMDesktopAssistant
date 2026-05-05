using LLMDesktopAssistant.Utils;
using ModelContextProtocol.Protocol;

namespace LLMDesktopAssistant.Agents.ExecutionStages
{
	public class RandomAgentExecutionStage : MentionableAgentExecutionStage
	{
		private readonly Random _random = new();

		protected override async Task<Guid?> SelectNextAgentAsync(List<AgentInstance> selectFrom, AgentPreExecutionContext context, CancellationToken cancellationToken = default)
		{
			if (await base.SelectNextAgentAsync(selectFrom, context, cancellationToken) is Guid nextAgent)
				return nextAgent;

			double weightSum = selectFrom.Sum(a => a.Weight);
			double randomValue = _random.NextDouble() * weightSum;
			double currentWeight = 0;

			foreach (var agent in selectFrom)
			{
				currentWeight += agent.Weight;
				if (currentWeight >= randomValue)
					return agent.AgentId;
			}

			return null;
		}
	}
}
