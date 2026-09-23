using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Services.Tools;

namespace LLMDesktopAssistant.Prompting.State
{
	/// <summary>
	/// The state of the tools section: the canonical tool definitions available to the agent.
	/// </summary>
	public class ToolsSectionState : PromptSectionStateBase
	{
		private List<SerializableToolDefinition> _tools = [];
		/// <summary>
		/// The tool definitions to inject into the system prompt.
		/// </summary>
		public List<SerializableToolDefinition> Tools
		{
			get => _tools;
			set => SetProperty(ref _tools, value);
		}
	}

	/// <summary>
	/// The delta of the tools section (stub).
	/// </summary>
	public class ToolsSectionDelta : PromptSectionDeltaBase
	{
	}

	/// <summary>
	/// Captures the tools section state by reading the current toolset cache.
	/// The cache is invalidated by the prompt composer, not by this provider.
	/// </summary>
	[ChatService(typeof(IPromptSectionStateProvider<ToolsSectionState>))]
	public class ToolsStateProvider(
		IToolsetCacheService toolsetCache) : IPromptSectionStateProvider<ToolsSectionState>
	{
		/// <inheritdoc/>
		public ToolsSectionState GetState(ChatAgentDescriptor agent)
		{
			var tools = toolsetCache.ValidTools.Values
				.Where(t => !(t.Hidden ?? false))
				.Select(SerializableToolDefinition.From)
				.ToList();

			return new ToolsSectionState
			{
				Tools = tools
			};
		}
	}

	/// <summary>
	/// Renders the tools section state into a snapshot (tools only, no text).
	/// </summary>
	[ChatService(typeof(IPromptSectionStateRenderer<ToolsSectionState>))]
	public class ToolsStateRenderer : IPromptSectionStateRenderer<ToolsSectionState>
	{
		/// <inheritdoc/>
		public SystemPromptSnapshot Render(ToolsSectionState state) => new()
		{
			Text = string.Empty,
			Tools = state.Tools
		};
	}

	/// <summary>
	/// Delta provider of the tools section (stub: no deltas yet).
	/// </summary>
	[ChatService(typeof(IPromptSectionDeltaProvider<ToolsSectionState, ToolsSectionDelta>))]
	public class ToolsDeltaProvider : IPromptSectionDeltaProvider<ToolsSectionState, ToolsSectionDelta>
	{
		/// <inheritdoc/>
		public ToolsSectionDelta? CalculateDelta(ToolsSectionState anchorState,
			IEnumerable<ToolsSectionDelta> existingDeltas, ToolsSectionState actualState) => null;
	}

	/// <summary>
	/// Delta renderer of the tools section (stub: no deltas yet).
	/// </summary>
	[ChatService(typeof(IPromptSectionDeltaRenderer<ToolsSectionDelta>))]
	public class ToolsDeltaRenderer : IPromptSectionDeltaRenderer<ToolsSectionDelta>
	{
		/// <inheritdoc/>
		public string Render(ToolsSectionDelta delta) => string.Empty;
	}

	/// <summary>
	/// The tools section: provides the tool definitions for the system prompt.
	/// </summary>
	[ChatService(typeof(IPromptSection))]
	public class ToolsSection(IServiceProvider services)
		: PromptSectionBase<ToolsSectionState, ToolsSectionDelta>(services)
	{
		/// <inheritdoc/>
		public override int Order => 100;
	}
}
