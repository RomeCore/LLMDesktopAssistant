namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// Interface for the agent ordering service.
	/// </summary>
	public interface IAgentOrderingService
	{
		/// <summary>
		/// Determines what agent should be executed next. Returns null if no agent should be executed.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
		/// <returns>The next agent ID to execute paired with stage ID, or null if no agent should be executed.</returns>
		Task<(Guid, Guid)?> GetNextAgentAsync(CancellationToken cancellationToken = default);
	}
}