using LLMDesktopAssistant.Agents.SubAgents;
using LLMDesktopAssistant.Agents.Tasks;
using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.LLM.Services.Tools
{
	/// <summary>
	/// The default implementation of the <see cref="ISubAgentToolResolver"/> interface.
	/// </summary>
	[ChatService(typeof(ISubAgentToolResolver))]
	public class SubAgentToolResolver(
		IToolsetCacheService toolsetCache
	) : ISubAgentToolResolver
	{
		/// <inheritdoc/>
		public IEnumerable<AgentTool> ResolveSubAgentTools(SubAgentInfo agentInfo)
		{
			// Tools are looked up in the aliased tool dictionary so that both primary names
			// and aliases (e.g. Claude Code compatible names) can be used in sub-agent definitions.
			var toolLookup = toolsetCache.AliasedTools;
			var resolved = new Dictionary<string, AgentTool>(StringComparer.Ordinal);

			// Available tools are added first with the lowest priority.
			foreach (var tool in agentInfo.AvailableTools)
			{
				if (toolLookup.TryGetValue(tool.ToolName, out var toolInfo) && !resolved.ContainsKey(toolInfo.Name))
					resolved[toolInfo.Name] = CreateAgentTool(toolInfo, ToolApprovalLevel.PolicyBased);
			}

			// Allowed tools override available tools.
			foreach (var tool in agentInfo.AllowedTools)
			{
				if (toolLookup.TryGetValue(tool.ToolName, out var toolInfo))
					resolved[toolInfo.Name] = CreateAgentTool(toolInfo, ToolApprovalLevel.AlwaysApprove);
			}

			// Disallowed tools override everything.
			foreach (var tool in agentInfo.DisallowedTools)
			{
				if (toolLookup.TryGetValue(tool.ToolName, out var toolInfo))
					resolved[toolInfo.Name] = CreateAgentTool(toolInfo, ToolApprovalLevel.AlwaysDisallow);
			}

			return resolved.Values;
		}

		private static AgentTool CreateAgentTool(ToolInfo toolInfo, ToolApprovalLevel approvalLevel)
		{
			return new ChatAgentTool(toolInfo, executionContext: null)
			{
				ApprovalLevel = approvalLevel
			};
		}
	}
}
