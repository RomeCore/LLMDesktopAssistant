namespace LLMDesktopAssistant.Agents.ExecutionStages
{
	/// <summary>
	/// The abstract/base class for execution stages where agents are executed multiple times.
	/// </summary>
	public abstract class RepeatableBaseAgentExecutionStage : AgentExecutionStage
	{
		private readonly Random _random = new();

		private bool _canAgentsExecuteAgain = true;
		/// <summary>
		/// Determines if agents can execute again after them is executed during this stage.
		/// </summary>
		public bool CanAgentsExecuteAgain
		{
			get => _canAgentsExecuteAgain;
			set => SetProperty(ref _canAgentsExecuteAgain, value);
		}

		private int _minIterations = -1;
		/// <summary>
		/// Minimum number of iterations to perform. If set to a negative value, this becomes the same as <see cref="MaxIterations"/>.
		/// </summary>
		public int MinIterations
		{
			get => _minIterations;
			set => SetProperty(ref _minIterations, value);
		}

		private int _maxIterations = -1;
		/// <summary>
		/// Maximum number of iterations to perform. If set to a negative value, there is no limit on the number of iterations.
		/// </summary>
		public int MaxIterations
		{
			get => _maxIterations;
			set => SetProperty(ref _maxIterations, value);
		}

		private double _stopChance = 0;
		/// <summary>
		/// Chance to stop the execution before reaching <see cref="MaxIterations"/>. If set to 0, no chance of stopping; if set to 1, guaranteed to stop on each iteration.
		/// </summary>
		public double StopChance
		{
			get => _stopChance;
			set => SetProperty(ref _stopChance, value);
		}

		protected abstract Task<Guid?> SelectNextAgentAsync(List<ChatAgentInstance> selectFrom,
			AgentPreExecutionContext context, CancellationToken cancellationToken = default);

		public override Task<Guid?> GetNextAgentAsync(AgentPreExecutionContext context,
			CancellationToken cancellationToken = default)
		{
			int iterationsCount = context.ExecutedInThisStage.Count;

			if (MaxIterations >= 0 && iterationsCount >= MaxIterations)
				return Task.FromResult<Guid?>(null);
			if (MinIterations >= 0 && iterationsCount >= MinIterations && _random.NextDouble() < StopChance)
				return Task.FromResult<Guid?>(null);

			var instances = AgentInstances.Where(a => a.Enabled);
			if (!CanAgentsExecuteAgain)
				instances = instances.ExceptBy(context.ExecutedInThisStage, a => a.AgentId);
			instances = instances.Where(a => a.AgentId != context.PreviousAgentExecuted);
			var instancesList = instances.ToList();

			if (instancesList.Count == 0)
				return Task.FromResult<Guid?>(null);

			return SelectNextAgentAsync(instancesList, context, cancellationToken);
		}
	}
}
