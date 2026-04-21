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
		/// <returns>A task representing the asynchronous operation.</returns>
		Task ExecuteAsync(AssistantMessage message, ToolCall toolCall, LLMInfo llmInfo, ImmutableDictionary<string, ToolInfo> tools, CancellationToken cancellationToken = default);
	}
}