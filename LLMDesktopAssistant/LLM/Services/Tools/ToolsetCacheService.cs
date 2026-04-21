using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.LLM.Services.Tools
{
	/// <summary>
	/// The default implementation of the <see cref="IToolsetCacheService"/> interface.
	/// </summary>
	[ChatService(typeof(IToolsetCacheService))]
	public class ToolsetCacheService(IToolsetBuildingService builder) : IToolsetCacheService
	{
		private ImmutableDictionary<string, ToolInfo> _availableTools = [], _validTools = [];

		public ImmutableDictionary<string, ToolInfo> AvailableTools => _availableTools;

		public ImmutableDictionary<string, ToolInfo> ValidTools => _validTools;

		public void Invalidate()
		{
			_availableTools = builder.GetAvailableTools().ToImmutableDictionary(t => t.Name);
			_validTools = builder.BuildTools().ToImmutableDictionary(t => t.Name);
		}
	}
}