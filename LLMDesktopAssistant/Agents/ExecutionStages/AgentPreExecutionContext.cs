using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;

namespace LLMDesktopAssistant.Agents.ExecutionStages
{
	/// <summary>
	/// Represents the context for selecting next agent inside execution stage before it starts.
	/// </summary>
	public sealed class AgentPreExecutionContext
	{
		/// <summary>
		/// The service provider that provides access to the current chat's services.
		/// </summary>
		public required IServiceProvider Services { get; init; }
		public Chat Chat => Services.GetRequiredService<Chat>();
		public IAgentManagementService AgentManager => Services.GetRequiredService<IAgentManagementService>();

		/// <summary>
		/// A list of user messages in this round.
		/// </summary>
		public required IReadOnlyList<UserMessage> ThisRoundUserMessages { get; init; }

		/// <summary>
		/// A list of assistant messages in this round.
		/// </summary>
		public required IReadOnlyList<AssistantMessage> ThisRoundAssistantMessages { get; init; }

		/// <summary>
		/// A list of previously executed agent IDs paired with the stage ID they were executed in.
		/// </summary>
		public required IReadOnlyList<(Guid AgentId, Guid StageId)> ExecutedInThisRound { get; init; }

		/// <summary>
		/// A list of messages in this execution stage.
		/// </summary>
		public required IReadOnlyList<AssistantMessage> ThisStageMessages { get; init; }

		/// <summary>
		/// A list of previously executed agent IDs in this stage.
		/// </summary>
		public required IReadOnlyList<Guid> ExecutedInThisStage { get; init; }

		/// <summary>
		/// A list of messages from previous agent.
		/// </summary>
		public required IReadOnlyList<AssistantMessage> PrevousAgentMessages { get; init; }

		/// <summary>
		/// The ID of the previously executed agent, this can be agent from previous stage. Use it to prevent same agent execution in a row.
		/// </summary>
		public required Guid? PrevousAgentExecuted { get; init; }
	}
}