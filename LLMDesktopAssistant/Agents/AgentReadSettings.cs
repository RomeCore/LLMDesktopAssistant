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
			AgentReadPermissions.OtherAgentContent |
			AgentReadPermissions.OtherAgentToolCalls |
			AgentReadPermissions.OtherAgentAttachments |
			AgentReadPermissions.MessagesWithToolCalls;
		/// <summary>
		/// The permissions that determines what the agent can read.
		/// </summary>
		public AgentReadPermissions ReadPermissions
		{
			get => _readPermissions;
			set => SetProperty(ref _readPermissions, value);
		}

		private AgentExposureMode _exposureMode =
			AgentExposureMode.Reasoning |
			AgentExposureMode.Content |
			AgentExposureMode.ToolCalls |
			AgentExposureMode.Attachments |
			AgentExposureMode.MessagesWithToolCalls;
		/// <summary>
		/// The exposure mode that determines what parts of this agent's messages
		/// are visible to other agents.
		/// </summary>
		public AgentExposureMode ExposureMode
		{
			get => _exposureMode;
			set => SetProperty(ref _exposureMode, value);
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

		private bool _isFilterWhiteList = false;
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
		/// <summary>
		/// The maximum number of rounds that the agent can see in its context.
		/// </summary>
		public int MaxVisibleRounds
		{
			get => _maxVisibleRounds;
			set => SetProperty(ref _maxVisibleRounds, value);
		}

		private bool _allowContextShields = true;
		/// <summary>
		/// Whether the agent can use context shields to prevent seeing messages after shields.
		/// </summary>
		public bool AllowContextShields
		{
			get => _allowContextShields;
			set => SetProperty(ref _allowContextShields, value);
		}

		private bool _allowSummaries = true;
		/// <summary>
		/// Whether the agent is allowed to see summaries of messages in chat history and stop on them.
		/// </summary>
		public bool AllowSummaries
		{
			get => _allowSummaries;
			set => SetProperty(ref _allowSummaries, value);
		}
	}
}
