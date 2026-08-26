using LLMDesktopAssistant.SourceGenerators;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Describes the agent-level sub-agent settings: the local enable flag and the inheritable
	/// list of per-sub-agent changes.
	/// </summary>
	[SettingsRoute(nameof(ChatAgentDescriptor.SubAgents))]
	public partial class AgentSubAgentSettings : AgentSettingsCategoryBase
	{
		private bool _enableSubAgents = true;
		/// <summary>
		/// Gets or sets a value indicating whether sub-agents are enabled for the agent.
		/// </summary>
		public bool EnableSubAgents
		{
			get => _enableSubAgents;
			set => SetProperty(ref _enableSubAgents, value);
		}

		private SubAgentsetSettings _subAgentset = new();
		/// <summary>
		/// Gets or sets the sub-agentset settings.
		/// </summary>
		[InheritedChatAgentSetting]
		public SubAgentsetSettings SubAgentset
		{
			get => _subAgentset;
			set => SetProperty(ref _subAgentset, value);
		}
	}
}
