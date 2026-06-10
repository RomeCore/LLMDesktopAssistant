using LLMDesktopAssistant.Tools;
using System.Collections.Immutable;

namespace LLMDesktopAssistant.LLM.Services.Tools
{
	/// <summary>
	/// The default implementation of the <see cref="IToolsetCacheService"/> interface.
	/// </summary>
	[ChatService(typeof(IToolsetCacheService))]
	public class ToolsetCacheService(IToolsetBuildingService builder) : IToolsetCacheService
	{
		private ImmutableDictionary<string, ToolInfo> _availableTools = [], _validTools = [], _validAliasedTools = [];

		public ImmutableDictionary<string, ToolInfo> AvailableTools => _availableTools;

		public ImmutableDictionary<string, ToolInfo> ValidTools => _validTools;

		public ImmutableDictionary<string, ToolInfo> ValidAliasedTools => _validAliasedTools;

		public void Invalidate(Guid agentId)
		{
			_availableTools = builder.GetAvailableTools().ToImmutableDictionary(t => t.Name);
			_validTools = builder.BuildTools(agentId).ToImmutableDictionary(t => t.Name);
			_validAliasedTools = BuildDictionaryWithAliases(builder.BuildTools(agentId));
		}

		/// <summary>
		/// Builds an immutable dictionary from a collection of tools, including both the primary name and all aliases as keys.
		/// If multiple tools share the same key (name or alias), the last one wins.
		/// </summary>
		private static ImmutableDictionary<string, ToolInfo> BuildDictionaryWithAliases(IEnumerable<ToolInfo> tools)
		{
			return tools
				.SelectMany(t => new[] { (Key: t.Name, Tool: t) }
					.Concat(t.Aliases.Select(a => (Key: a, Tool: t))))
				.GroupBy(x => x.Key)
				.Select(g => g.Last())
				.ToImmutableDictionary(x => x.Key, x => x.Tool);
		}
	}
}
