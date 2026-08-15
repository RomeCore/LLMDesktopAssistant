using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.SourceGenerators;
using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Describes an agent tool settings.
	/// Contains the tool policy group, the toolset group and the local tools enable flag.
	/// </summary>
	[SettingsRoute(nameof(ChatAgentDescriptor.Tools))]
	public partial class AgentToolSettings : AgentSettingsCategoryBase
	{
		private bool _enableTools = true;
		/// <summary>
		/// Whether to use tools in the chat.
		/// </summary>
		public bool EnableTools
		{
			get => _enableTools;
			set => SetProperty(ref _enableTools, value);
		}

		private ToolPolicySettings _policy = new();
		/// <summary>
		/// Gets or sets the tool behaviour policy for this agent.
		/// Inherits the chat profile policy by default.
		/// </summary>
		[InheritedChatAgentSetting(DefaultLevel = ChatSettingsInheritanceLevel.Profile)]
		public ToolPolicySettings Policy
		{
			get => _policy;
			set => SetProperty(ref _policy, value);
		}

		private ToolsetSettings _toolset = new();
		/// <summary>
		/// Gets or sets the toolset configuration for this agent: a custom toolset or a
		/// reference to a shared toolset configuration.
		/// </summary>
		[InheritedChatAgentSetting]
		public ToolsetSettings Toolset
		{
			get => _toolset;
			set => SetProperty(ref _toolset, value);
		}
	}
}
