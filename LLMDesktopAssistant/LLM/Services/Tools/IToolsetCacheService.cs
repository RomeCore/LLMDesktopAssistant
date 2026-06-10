using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.LLM.Services.Tools
{
	/// <summary>
	/// The service responsible for caching and managing the toolset.
	/// </summary>
	public interface IToolsetCacheService
	{
		/// <summary>
		/// Gets the current tools that are available in the chat session.
		/// </summary>
		ImmutableDictionary<string, ToolInfo> AvailableTools { get; }

		/// <summary>
		/// Gets the current tools that are available to the LLM inference.
		/// </summary>
		ImmutableDictionary<string, ToolInfo> ValidTools { get; }

		/// <summary>
		/// Gets the current tools that are available to the LLM inference. Used for tool lookup.
		/// </summary>
		ImmutableDictionary<string, ToolInfo> ValidAliasedTools { get; }

		/// <summary>
		/// Invalidates the cache and refreshes it.
		/// </summary>
		/// <param name="agentId">The ID of the agent for which to invalidate the cache.</param>
		void Invalidate(Guid agentId);
	}
}