using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Describes an agent's reading settings.
	/// </summary>
	public class AgentReadSettings : NotifyPropertyChanged
	{
		private AgentReadPermissions _readPermissions =
			AgentReadPermissions.UserMessages |
			AgentReadPermissions.UserAttachments |
			AgentReadPermissions.OwnMessages |
			AgentReadPermissions.OtherAgentMessages |
			AgentReadPermissions.OtherAgentToolCalls;
		/// <summary>
		/// The permissions that determines what the agent can read.
		/// </summary>
		public AgentReadPermissions ReadPermissions
		{
			get => _readPermissions;
			set => SetProperty(ref _readPermissions, value);
		}

		private RangeObservableCollection<Guid> _agentIdsReadFilter = [];
		/// <summary>
		/// The list of agent IDs that the agent can read.
		/// The behaviour of filter is controlled by <see cref="IsFilterWhiteList"/>.
		/// If empty, all agents are readable.
		/// </summary>
		public ICollection<Guid> AgentIdsReadFilter
		{
			get => _agentIdsReadFilter;
			set => _agentIdsReadFilter.Reset(value);
		}

		private bool _isFilterWhiteList = true;
		/// <summary>
		/// Whether the filter is a white list or black list.
		/// If true, only agents in the <see cref="AgentIdsReadFilter"/> can be read. If false, all agents except those in the filter can be read.
		/// </summary>
		public bool IsFilterWhiteList
		{
			get => _isFilterWhiteList;
			set => SetProperty(ref _isFilterWhiteList, value);
		}

		private int _maxVisibleRounds = 0;
		public int MaxVisibleRounds
		{
			get => _maxVisibleRounds;
			set => SetProperty(ref _maxVisibleRounds, value);
		}
	}
}