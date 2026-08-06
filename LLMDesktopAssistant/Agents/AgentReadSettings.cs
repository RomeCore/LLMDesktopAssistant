using LLMDesktopAssistant.SourceGenerators;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Describes an agent's reading settings: the inheritable reading, exposure and context
	/// groups, and the local agent ID filter.
	/// </summary>
	[SettingsRoute(nameof(ChatAgentDescriptor.Read))]
	public partial class AgentReadSettings : AgentSettingsCategoryBase
	{
		private AgentReadingSettings _reading = new();
		/// <summary>
		/// Gets or sets the reading permissions group: what the agent can read.
		/// </summary>
		[InheritedChatAgentSetting]
		public AgentReadingSettings Reading
		{
			get => _reading;
			set => SetProperty(ref _reading, value);
		}

		private AgentExposureSettings _exposure = new();
		/// <summary>
		/// Gets or sets the exposure group: what parts of this agent's messages
		/// are visible to other agents.
		/// </summary>
		[InheritedChatAgentSetting]
		public AgentExposureSettings Exposure
		{
			get => _exposure;
			set => SetProperty(ref _exposure, value);
		}

		private AgentContextSettings _context = new();
		/// <summary>
		/// Gets or sets the context group: visible rounds, context shields and summaries.
		/// </summary>
		[InheritedChatAgentSetting]
		public AgentContextSettings Context
		{
			get => _context;
			set => SetProperty(ref _context, value);
		}

		private readonly RangeObservableCollection<Guid> _agentIdsReadFilter = [];
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
	}
}
