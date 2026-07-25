using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.Agents.Tasks
{
	public class AgentTaskLaunchParameters
	{
		/// <summary>
		/// The name of the task to be launched. Used for identifying by the user in the dispatcher.
		/// </summary>
		public required string? TaskName { get; init; }

		/// <summary>
		/// The full name of the model to be used for the agent task.
		/// </summary>
		public required string ModelName { get; init; }

		/// <summary>
		/// The initial set of chat messages to be used for the agent task.
		/// </summary>
		public required ImmutableList<AgentChatMessage> InitialMessages { get; init; }

		/// <summary>
		/// The list of tools available to the agent during the task.
		/// </summary>
		public ImmutableList<AgentTool> Tools { get; init; } = [];

		/// <summary>
		/// The behavior of tools that are automatically approved.
		/// </summary>
		public ToolBehaviour AutoApproveBehaviours { get; init; }

		/// <summary>
		/// The behavior of tools that are disallowed.
		/// </summary>
		public ToolBehaviour DisallowedBehaviours { get; init; }
	}
}
