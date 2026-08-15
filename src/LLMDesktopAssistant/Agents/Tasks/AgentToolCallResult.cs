namespace LLMDesktopAssistant.Agents.Tasks
{
	public class AgentToolCallResult
	{
		public required string Content { get; init; }

		public required bool Success { get; init; }

		public ImmutableList<AgentAttachment> Attachments { get; init; } = [];
	}
}
