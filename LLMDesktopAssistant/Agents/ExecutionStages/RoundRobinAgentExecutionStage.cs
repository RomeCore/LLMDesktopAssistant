namespace LLMDesktopAssistant.Agents.ExecutionStages
{
	/// <summary>
	/// An execution stage that selects agents in a round-robin fashion,
	/// cycling through all enabled agents sequentially and wrapping around
	/// when the end of the list is reached. Supports minimum/maximum number
	/// of full cycles with an optional stop chance after each completed cycle.
	/// When <see cref="CanAgentsBeSkipped"/> is enabled, each agent's <see cref="AgentInstance.Weight"/>
	/// is used as the probability of being selected (skip chance = 1 - weight).
	/// </summary>
	public class RoundRobinAgentExecutionStage : AgentExecutionStage
	{
		private readonly Random _random = new();

		private bool _canAgentsBeSkipped = false;
		/// <summary>
		/// Whether agents can be skipped based on their <see cref="AgentInstance.Weight"/>.
		/// When enabled, an agent's weight represents the probability of being selected
		/// (0 = always skipped, 1 = never skipped).
		/// </summary>
		public bool CanAgentsBeSkipped
		{
			get => _canAgentsBeSkipped;
			set => SetProperty(ref _canAgentsBeSkipped, value);
		}

		private int _minCycles = -1;
		/// <summary>
		/// Minimum number of full round-robin cycles to perform.
		/// If set to a negative value, this becomes the same as <see cref="MaxCycles"/>.
		/// </summary>
		public int MinCycles
		{
			get => _minCycles;
			set => SetProperty(ref _minCycles, value);
		}

		private int _maxCycles = -1;
		/// <summary>
		/// Maximum number of full round-robin cycles to perform.
		/// If set to a negative value, there is no limit on the number of cycles.
		/// </summary>
		public int MaxCycles
		{
			get => _maxCycles;
			set => SetProperty(ref _maxCycles, value);
		}

		private double _stopChance = 0;
		/// <summary>
		/// Chance to stop the execution after completing a full cycle,
		/// once <see cref="MinCycles"/> have been reached.
		/// If set to <c>0</c>, no chance of stopping; if set to <c>1</c>, guaranteed to stop after each cycle.
		/// </summary>
		public double StopChance
		{
			get => _stopChance;
			set => SetProperty(ref _stopChance, value);
		}

		/// <inheritdoc />
		public override Task<Guid?> GetNextAgentAsync(AgentPreExecutionContext context,
			CancellationToken cancellationToken = default)
		{
			var agentsList = AgentInstances.Where(a => a.Enabled).ToList();
			if (agentsList.Count == 0)
				return Task.FromResult<Guid?>(null);

			int executedCount = context.ExecutedInThisStage.Count;
			int completedCycles = executedCount / agentsList.Count;

			// If we've reached MaxCycles, stop
			if (_maxCycles >= 0 && completedCycles >= _maxCycles)
				return Task.FromResult<Guid?>(null);

			// If we've reached MinCycles, check stop chance at the start of a new cycle
			if (_minCycles >= 0 && completedCycles >= _minCycles
				&& executedCount % agentsList.Count == 0
				&& _random.NextDouble() < _stopChance)
			{
				return Task.FromResult<Guid?>(null);
			}

			// Determine starting index: right after the last agent selected by this stage
			var lastSelectedByThisStage = context.ExecutedInThisStage.Count > 0
				? context.ExecutedInThisStage[^1]
				: (Guid?)null;

			int startIndex = 0;
			if (lastSelectedByThisStage.HasValue)
			{
				int lastIndex = agentsList.FindIndex(a => a.AgentId == lastSelectedByThisStage.Value);
				if (lastIndex >= 0)
					startIndex = (lastIndex + 1) % agentsList.Count;
			}

			// Try to find the next agent, potentially skipping based on weight
			for (int i = 0; i < agentsList.Count; i++)
			{
				int candidateIndex = (startIndex + i) % agentsList.Count;
				var candidate = agentsList[candidateIndex];

				// Always skip the previously executed agent (from any stage)
				if (candidate.AgentId == context.PreviousAgentExecuted)
					continue;

				// If weight-based skipping is enabled, apply skip chance
				if (_canAgentsBeSkipped)
				{
					double weight = Math.Clamp(candidate.Weight, 0.0, 1.0);
					if (_random.NextDouble() >= weight)
						continue;
				}

				return Task.FromResult<Guid?>(candidate.AgentId);
			}

			// Fallback: if all agents were skipped, pick the first eligible one ignoring weights
			foreach (var candidate in agentsList)
			{
				if (candidate.AgentId == context.PreviousAgentExecuted)
					continue;
				return Task.FromResult<Guid?>(candidate.AgentId);
			}

			return Task.FromResult<Guid?>(null);
		}
	}
}
