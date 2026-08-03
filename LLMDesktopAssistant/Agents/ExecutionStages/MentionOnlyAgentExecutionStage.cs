using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.Agents.ExecutionStages
{
	[JsonDerived(typeof(AgentExecutionStage), "mentionOnly")]
	public class MentionOnlyAgentExecutionStage : MentionableBaseAgentExecutionStage
	{
	}
}