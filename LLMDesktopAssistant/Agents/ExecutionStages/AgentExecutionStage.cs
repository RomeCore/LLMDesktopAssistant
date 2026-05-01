using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace LLMDesktopAssistant.Agents.ExecutionStages
{
	[JsonDerivedType(typeof(AgentExecutionSequentialStage), "sequential")]
	[JsonDerivedType(typeof(AgentExecutionRandomStage), "random")]
	public abstract class AgentExecutionStage : NotifyPropertyChanged
	{
		private Guid _id;
		public Guid Id
		{
			get => _id;
			set => SetProperty(ref _id, value);
		}

		private bool _enabled = true;
		public bool Enabled
		{
			get => _enabled;
			set => SetProperty(ref _enabled, value);
		}

		[JsonIgnore]
		public abstract IReadOnlyList<AgentInstance> Children { get; }

		/// <summary>
		/// Returns the next agent ID to execute based on the provided context.
		/// </summary>
		/// <param name="context">The context for selecting the next agent.</param>
		/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
		/// <returns>The next agent ID to execute or null if no suitable agent is found and execution should go to the next stage.</returns>
		public abstract Task<Guid?> GetNextAgentAsync(AgentPreExecutionContext context, CancellationToken cancellationToken = default);
	}
}