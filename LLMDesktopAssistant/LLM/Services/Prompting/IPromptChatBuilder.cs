using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using RCLargeLanguageModels.Messages;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// Interface for prompt chat builder. This service is responsible for building the list of messages for the LLM chat input.
	/// </summary>
	public interface IPromptChatBuilder
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
		/// Builds a list of messages for the LLM chat input.
		/// </summary>
		/// <param name="agent">The agent to build the message list for.</param>
		/// <returns>A list of messages for the LLM chat input.</returns>
		IEnumerable<IMessage> Build(ChatAgentDescriptor agent);
	}
}
