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
		/// Converts a domain message to a list of messages for the LLM chat input,
		/// respecting the read permissions of the target agent.
		/// </summary>
		/// <param name="message">The domain message to convert.</param>
		/// <param name="agentId">The ID of the agent that will receive this message.</param>
		/// <returns>A list of messages for the LLM chat input.
		/// For user message there will be single message.
		/// For own assistant message there will be at least one message with tool messages appended.
		/// For foreign assistant message it will be merged into a single user message.</returns>
		IEnumerable<IMessage> ConvertMessage(ChatMessage message, Guid agentId);

		/// <summary>
		/// Converts a message to LLM messages without applying agent-specific visibility filters.
		/// Used for summarization and other background processes.
		/// </summary>
		IEnumerable<IMessage> ConvertMessageUnsafe(ChatMessage message);

		/// <summary>
		/// Builds a list of messages for the LLM chat input.
		/// </summary>
		/// <param name="agentId">The ID of the agent to build the message list for.</param>
		/// <returns>A list of messages for the LLM chat input.</returns>
		IEnumerable<IMessage> Build(Guid agentId);
	}
}
