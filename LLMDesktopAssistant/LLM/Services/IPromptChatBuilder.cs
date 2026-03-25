using RCLargeLanguageModels.Messages;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// Interface for prompt chat builder. This service is responsible for building the list of messages for the LLM chat input.
	/// </summary>
	public interface IPromptChatBuilder
	{
		/// <summary>
		/// Builds a list of messages for the LLM chat input.
		/// </summary>
		/// <returns>A list of messages for the LLM chat input.</returns>
		IEnumerable<IMessage> Build();
	}
}