namespace LLMDesktopAssistant.Agents.Tasks
{
	public class TaskSubAgentDescriptor
	{
		/// <summary>
		/// The name of the sub-agent.
		/// </summary>
		public required string Name { get; init; }

		/// <summary>
		/// The description of the sub-agent.
		/// </summary>
		public required string Description { get; init; }
	}
}
