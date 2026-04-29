using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// Interface for LLM provider.
	/// </summary>
	public interface ILLMBuildingService 
	{
		/// <summary>
		/// Gets the chat large language model.
		/// </summary>
		/// <param name="agentId">The agent identifier to build LLM for.</param>
		/// <returns>The chat large language model.</returns>
		LLMInfo? BuildChatLLM(Guid agentId);

		/// <summary>
		/// Gets the large language model for summarization.
		/// </summary>
		/// <returns>The large language model for summarization.</returns>
		LLMInfo? BuildSummarizationLLM();
	}
}