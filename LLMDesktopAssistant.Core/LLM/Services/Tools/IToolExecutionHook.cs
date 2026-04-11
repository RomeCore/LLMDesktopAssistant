using LLMDesktopAssistant.Core.LLM.Domain;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Core.LLM.Services.Tools
{
	public interface IToolExecutionHook
	{
		Task<ToolResult?> OnBeforeExecuteAsync(ToolCall toolCall, LLMInfo llmInfo, CancellationToken cancellationToken = default);
	}
}