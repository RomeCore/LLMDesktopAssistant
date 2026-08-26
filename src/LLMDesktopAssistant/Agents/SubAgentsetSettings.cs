using LLMDesktopAssistant.Agents.SubAgents;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents
{
	public class SubAgentsetSettings : NotifyPropertyChanged
	{
		private bool _subAgentsEnabledByDefault = true;
		/// <summary>
		/// Gets or sets a value indicating whether unchanged sub-agents are enabled by default.
		/// </summary>
		public bool SubAgentsEnabledByDefault
		{
			get => _subAgentsEnabledByDefault;
			set => SetProperty(ref _subAgentsEnabledByDefault, value);
		}

		private readonly RangeObservableCollection<SubAgentChange> _subAgentChanges = [];
		/// <summary>
		/// Gets or sets the sub-agent changes compared to all available sub-agents.
		/// </summary>
		public RangeObservableCollection<SubAgentChange> SubAgentChanges
		{
			get => _subAgentChanges;
			set => _subAgentChanges.Reset(value);
		}
	}
}
