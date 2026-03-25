using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// Interface for LLM provider.
	/// </summary>
	public interface ILLMProvider 
	{
		/// <summary>
		/// Gets the chat large language model.
		/// </summary>
		/// <returns>The chat large language model.</returns>
		LLMInfo GetChatLLM();

		/// <summary>
		/// Gets the large language model for summarization.
		/// </summary>
		/// <returns>The large language model for summarization.</returns>
		LLMInfo GetSummarizationLLM();
	}
}