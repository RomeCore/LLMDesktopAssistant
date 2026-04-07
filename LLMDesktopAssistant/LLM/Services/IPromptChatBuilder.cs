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
		/// Converts a domain message to a list of messages for the LLM chat input.
		/// </summary>
		/// <param name="message">The domain message to convert.</param>
		/// <returns>A list of messages for the LLM chat input.
		/// For user message there will be single message.
		/// For assistant message there will be at least one message with tool messages appended.</returns>
		IEnumerable<IMessage> ConvertMessage(ChatMessage message);

		/// <summary>
		/// Builds a list of messages for the LLM chat input.
		/// </summary>
		/// <returns>A list of messages for the LLM chat input.</returns>
		IEnumerable<IMessage> Build();
	}
}