using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// Interface for chat execution service.
	/// </summary>
	public interface IChatExecutionService
	{
		/// <summary>
		/// Generates a response to the provided user message asynchronously.
		/// </summary>
		/// <remarks>
		/// Implementations should add messages to the <see cref="IChatStorageService"/>.
		/// </remarks>
		/// <param name="userMessage">The user message to process.</param>
		/// <param name="cancellationToken">Token for cancellation of the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task GenerateResponseAsync(UserMessage userMessage, CancellationToken cancellationToken = default);
	}
}