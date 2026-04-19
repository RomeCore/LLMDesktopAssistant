namespace LLMDesktopAssistant.LLM.Services
{
	public interface ITokenCounterService
	{
		/// <summary>
		/// Counts the number of tokens in the provided text asynchronously.
		/// </summary>
		/// <param name="text">The text to count tokens in.</param>
		/// <param name="cancellationToken">Token for cancellation of the operation.</param>
		/// <returns>A task representing the asynchronous operation that returns the token count.</returns>
		Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default);
	}
}