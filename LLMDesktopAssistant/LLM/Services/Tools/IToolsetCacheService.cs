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
		public ImmutableDictionary<string, ToolInfo> AvailableTools { get; }

		/// <summary>
		/// Gets the current tools that are available to the LLM inference.
		/// </summary>
		public ImmutableDictionary<string, ToolInfo> ValidTools { get; }

		/// <summary>
		/// Invalidates the cache and refreshes it.
		/// </summary>
		public void Invalidate();
	}
}