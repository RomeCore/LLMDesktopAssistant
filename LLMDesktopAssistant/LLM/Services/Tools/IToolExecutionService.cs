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
		/// <param name="toolCall">The tool call to execute.</param>
		/// <param name="llmInfo">The information about the language model to use.</param>
		/// <param name="cancellationToken">Token for cancellation of the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task ExecuteAsync(ToolCall toolCall, LLMInfo llmInfo, ImmutableDictionary<string, ToolInfo> tools, CancellationToken cancellationToken = default);
	}
}