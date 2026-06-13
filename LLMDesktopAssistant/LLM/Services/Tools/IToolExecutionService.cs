using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Tools;
using RCLargeLanguageModels.Tools;
using System.Collections.Immutable;

namespace LLMDesktopAssistant.LLM.Services.Tools
{
	/// <summary>
	/// Interface for tool execution service.
	/// </summary>
	public interface IToolExecutionService
	{
		/// <summary>
		/// Executes a tool call asynchronously.
		/// </summary>
		Task ExecuteAsync(PartialFunctionToolCall? partialFunctionToolCall,
			AssistantMessage message, ToolCall toolCall, ToolInfo? toolInfo, CancellationToken cancellationToken = default);
	}
}