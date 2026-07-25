using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.Agents.Tasks
{
	public class AgentToolCallPreResult
	{
		public required ToolBehaviour ExpectedBehaviour { get; init; }

		public bool? InterruptingSuccess { get; init; }

		public string? InterruptingContent { get; init; }

		public ImmutableList<AgentAttachment> InterruptingAttachments { get; init; } = [];

		public object? SharedContext { get; init; }
	}
}
