using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using RCLargeLanguageModels.Messages;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <summary>
	/// Interface for prompt chat builder. This service is responsible for building the list of messages for the LLM chat input.
	/// </summary>
	public interface IChatPromptBuilder
	{
		/// <summary>
		/// Converts a message to LLM messages without applying agent-specific visibility filters.
		/// Used for summarization and other background processes.
		/// </summary>
		IEnumerable<IMessage> ConvertMessage(BranchedMessage message);

		/// <summary>
		/// Converts a message to string without applying agent-specific visibility filters.
		/// </summary>
		string RenderMessage(BranchedMessage message);

		/// <summary>
		/// Renders a system prompt for the given agent.
		/// </summary>
		/// <param name="agent">The agent to render the system prompt for.</param>
		/// <param name="summaryOfPrevMessages">Optional summary of latest messages to include in the system prompt.</param>
		/// <returns>The rendered system prompt.</returns>
		string RenderSystemPrompt(ChatAgentDescriptor agent, string? summaryOfPrevMessages = null);

		/// <summary>
		/// Builds a list of messages for the LLM chat input.
		/// </summary>
		/// <param name="agent">The agent to build the message list for.</param>
		/// <returns>A list of messages for the LLM chat input.</returns>
		IEnumerable<IMessage> Build(ChatAgentDescriptor agent);
	}
}
