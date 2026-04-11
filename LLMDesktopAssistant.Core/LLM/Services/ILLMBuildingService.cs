using LLMDesktopAssistant.Core.LLM.Domain;

namespace LLMDesktopAssistant.Core.LLM.Services
{
	/// <summary>
	/// Interface for LLM provider.
	/// </summary>
	public interface ILLMBuildingService 
	{
		/// <summary>
		/// Gets the chat large language model.
		/// </summary>
		/// <returns>The chat large language model.</returns>
		LLMInfo? BuildChatLLM();

		/// <summary>
		/// Gets the large language model for summarization.
		/// </summary>
		/// <returns>The large language model for summarization.</returns>
		LLMInfo? BuildSummarizationLLM();
	}
}