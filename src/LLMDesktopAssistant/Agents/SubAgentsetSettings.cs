using LLMDesktopAssistant.Agents.SubAgents;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// The per-agent sub-agent set settings: the default enable/hide flags and the
	/// list of per-sub-agent changes.
	/// </summary>
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

		private bool _subAgentsHiddenByDefault = false;
		/// <summary>
		/// Gets or sets a value indicating whether unchanged sub-agents are hidden by default.
		/// </summary>
		public bool SubAgentsHiddenByDefault
		{
			get => _subAgentsHiddenByDefault;
			set => SetProperty(ref _subAgentsHiddenByDefault, value);
		}

		/// <summary>
		/// Gets or sets the sub-agent changes compared to all available sub-agents.
		/// </summary>
		public RangeObservableCollection<SubAgentChange> SubAgentChanges
		{
			get => field ??= [];
			set => (field ??= []).Reset(value);
		}
	}
}
