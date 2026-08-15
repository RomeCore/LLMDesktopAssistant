namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// Service for auto-naming chat conversations and generating topic categories.
	/// </summary>
	public interface IChatNamingService
	{
		/// <summary>
		/// Tries to auto-name the chat if conditions are met (chat has messages, title is still default, etc.).
		/// Will also generate a topic for the chat if not already set.
		/// </summary>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task TryNameChatAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Forces naming of the chat regardless of current state.
		/// Generates both title and topic using the LLM.
		/// </summary>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>True if naming was successful, false otherwise.</returns>
		Task<bool> NameChatAsync(CancellationToken cancellationToken = default);
	}
}
