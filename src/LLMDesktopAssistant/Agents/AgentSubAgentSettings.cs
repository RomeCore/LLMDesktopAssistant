using LLMDesktopAssistant.Agents.SubAgents;
using LLMDesktopAssistant.SourceGenerators;
using LLMDesktopAssistant.Utils;

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

		private readonly RangeObservableCollection<SubAgentChange> _subAgentChanges = [];
		/// <summary>
		/// Gets or sets the sub-agent changes compared to all available sub-agents.
		/// </summary>
		[InheritedChatAgentSetting]
		public RangeObservableCollection<SubAgentChange> SubAgentChanges
		{
			get => _subAgentChanges;
			set => _subAgentChanges.Reset(value);
		}
	}
}
