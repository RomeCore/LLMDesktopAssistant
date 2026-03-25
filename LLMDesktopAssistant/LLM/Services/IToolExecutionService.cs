using LLMDesktopAssistant.LLM.Domain;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.LLM.Services
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
		Task ExecuteAsync(ToolCall toolCall, LLMInfo llmInfo, CancellationToken cancellationToken = default);
	}
}