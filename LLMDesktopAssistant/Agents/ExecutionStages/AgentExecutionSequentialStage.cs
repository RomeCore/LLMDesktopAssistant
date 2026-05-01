using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents.ExecutionStages
{
	public class AgentExecutionSequentialStage : AgentExecutionStage
	{
		private RangeObservableCollection<AgentInstance> _agentInstances = [];
		public RangeObservableCollection<AgentInstance> AgentInstances
		{
			get => _agentInstances;
			set => _agentInstances.Reset(value);
		}

		public override Task<Guid?> GetNextAgentAsync(AgentPreExecutionContext context,
			CancellationToken cancellationToken = default)
		{
			var agentsList = AgentInstances.ToList();
			if (agentsList.Count == 0)
				return Task.FromResult<Guid?>(null);

			Guid? previousId = context.PrevousAgentExecuted;
			int startIndex = 0;

			if (previousId.HasValue)
			{
				int prevIndex = agentsList.FindIndex(a => a.AgentId == previousId.Value);
				if (prevIndex >= 0)
					startIndex = prevIndex + 1;
			}

			for (int i = startIndex; i < agentsList.Count; i++)
			{
				var agent = agentsList[i];
				if (!agent.Enabled)
					continue;
				if (agent.AgentId == previousId)
					continue;
				return Task.FromResult<Guid?>(agent.AgentId);
			}

			return Task.FromResult<Guid?>(null);
		}
	}
}
