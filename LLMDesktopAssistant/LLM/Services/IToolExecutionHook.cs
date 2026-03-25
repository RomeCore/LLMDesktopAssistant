using LLMDesktopAssistant.LLM.Domain;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.LLM.Services
{
	public interface IToolExecutionHook
	{
		Task<ToolResult?> OnBeforeExecuteAsync(ToolCall toolCall, LLMInfo llmInfo, CancellationToken cancellationToken = default);
	}
}