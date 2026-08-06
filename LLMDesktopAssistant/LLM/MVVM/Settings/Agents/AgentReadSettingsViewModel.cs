using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Settings;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents
{
	public class ReadPermissionItem : ObservableObject
	{
		private readonly AgentReadSettingsViewModel _parent;
		public AgentReadPermissions Permission { get; }
		public string DisplayName { get; }
		public string Description { get; }

		private bool _isEnabled;
		public bool IsEnabled
		{
			get => _isEnabled;
			set
			{
				if (SetProperty(ref _isEnabled, value))
				{
					_parent.EffectiveReading.ReadPermissions = (_parent.EffectiveReading.ReadPermissions & ~Permission) | (value ? Permission : 0);
				}
			}
		}

		public ReadPermissionItem(AgentReadSettingsViewModel parent, AgentReadPermissions permission, string displayName, string description, bool isEnabled)
		{
			_parent = parent;
			Permission = permission;
			DisplayName = displayName;
			Description = description;
			_isEnabled = isEnabled;
		}
	}

	public class ExposureModeItem : ObservableObject
	{
		private readonly AgentReadSettingsViewModel _parent;
		public AgentExposureMode Mode { get; }
		public string DisplayName { get; }
		public string Description { get; }

		private bool _isEnabled;
		public bool IsEnabled
		{
			get => _isEnabled;
			set
			{
				if (SetProperty(ref _isEnabled, value))
				{
					_parent.EffectiveExposure.ExposureMode = (_parent.EffectiveExposure.ExposureMode & ~Mode) | (value ? Mode : 0);
				}
			}
		}

		public ExposureModeItem(AgentReadSettingsViewModel parent, AgentExposureMode mode, string displayName, string description, bool isEnabled)
		{
			_parent = parent;
			Mode = mode;
			DisplayName = displayName;
			Description = description;
			_isEnabled = isEnabled;
		}
	}

	/// <summary>
	/// ViewModel for an agent entry in the read filter list.
	/// </summary>
	public class AgentFilterItem : ObservableObject
	{
		private readonly AgentReadSettingsViewModel _parent;
		public ChatAgentDescriptor Agent { get; }
		public string DisplayName => Agent.Info.Name ?? "Unnamed Agent";
		public bool IsGlobal { get; }

		private bool _isSelected;
		/// <summary>
		/// Whether this agent is selected in the filter (present in AgentIdsReadFilter).
		/// </summary>
		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (SetProperty(ref _isSelected, value))
				{
					_parent.UpdateAgentFilter();
				}
			}
		}

		public AgentFilterItem(AgentReadSettingsViewModel parent, ChatAgentDescriptor agent, bool isGlobal, bool isSelected)
		{
			_parent = parent;
			Agent = agent;
			IsGlobal = isGlobal;
			_isSelected = isSelected;
		}
	}

	[ViewModelFor(typeof(AgentReadSettingsView))]
	public class AgentReadSettingsViewModel : ViewModelBase
	{
		private readonly ICollection<ChatAgentDescriptor> _chatAgents;
		private readonly Guid _agentId;
		private readonly ChatSettings _chatSettings;

		/// <summary>
		/// Gets the underlying agent read settings.
		/// </summary>
		public AgentReadSettings ReadSettings { get; }

		/// <summary>
		/// Gets the effective reading permissions group resolved by the current inheritance level.
		/// </summary>
		public AgentReadingSettings EffectiveReading => ReadSettings.GetEffectiveReading(_chatSettings);

		/// <summary>
		/// Gets the effective exposure group resolved by the current inheritance level.
		/// </summary>
		public AgentExposureSettings EffectiveExposure => ReadSettings.GetEffectiveExposure(_chatSettings);

		/// <summary>
		/// Gets the effective context group resolved by the current inheritance level.
		/// </summary>
		public AgentContextSettings EffectiveContext => ReadSettings.GetEffectiveContext(_chatSettings);

		private InheritanceLevelItem _selectedReadingInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the reading group.
		/// </summary>
		public InheritanceLevelItem SelectedReadingInheritance
		{
			get => _selectedReadingInheritance;
			set
			{
				if (SetProperty(ref _selectedReadingInheritance, value) && value != null)
					ReadSettings.ReadingInheritance = value.Value;
			}
		}

		private InheritanceLevelItem _selectedExposureInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the exposure group.
		/// </summary>
		public InheritanceLevelItem SelectedExposureInheritance
		{
			get => _selectedExposureInheritance;
			set
			{
				if (SetProperty(ref _selectedExposureInheritance, value) && value != null)
					ReadSettings.ExposureInheritance = value.Value;
			}
		}

		private InheritanceLevelItem _selectedContextInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the context group.
		/// </summary>
		public InheritanceLevelItem SelectedContextInheritance
		{
			get => _selectedContextInheritance;
			set
			{
				if (SetProperty(ref _selectedContextInheritance, value) && value != null)
					ReadSettings.ContextInheritance = value.Value;
			}
		}

		public ObservableCollection<ReadPermissionItem> ReadPermissionItems { get; } = [];
		public ObservableCollection<ExposureModeItem> ExposureModeItems { get; } = [];

		/// <summary>
		/// Filter mode: 0 = Whitelist, 1 = Blacklist
		/// </summary>
		public int FilterModeIndex
		{
			get => ReadSettings.IsFilterWhiteList ? 0 : 1;
			set
			{
				ReadSettings.IsFilterWhiteList = value == 0;
				RaisePropertyChanged(null);
			}
		}

		/// <summary>
		/// List of all available agents with checkboxes for filter selection.
		/// </summary>
		public ObservableCollection<AgentFilterItem> AgentFilterItems { get; } = [];

		/// <summary>
		/// Whether the filter has any effect (whitelist with selected agents or blacklist with selected agents).
		/// </summary>
		public bool HasFilter => AgentFilterItems.Any(a => a.IsSelected);

		public bool IsWhitelistWithSelection => ReadSettings.IsFilterWhiteList && HasFilter;
		public bool IsBlacklistWithSelection => !ReadSettings.IsFilterWhiteList && HasFilter;

		public ICommand SelectAllAgentsCommand { get; }
		public ICommand DeselectAllAgentsCommand { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="AgentReadSettingsViewModel"/> class.
		/// </summary>
		/// <param name="settings">The agent read settings to edit.</param>
		/// <param name="chatAgents">The chat-local agent descriptors.</param>
		/// <param name="agentId">The ID of the agent being edited.</param>
		/// <param name="chatSettings">The chat settings used to resolve inherited settings.</param>
		public AgentReadSettingsViewModel(AgentReadSettings settings,
			ICollection<ChatAgentDescriptor> chatAgents, Guid agentId, ChatSettings chatSettings)
		{
			ReadSettings = settings;
			_chatAgents = chatAgents;
			_agentId = agentId;
			_chatSettings = chatSettings;

			_selectedReadingInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.ReadingInheritance);
			_selectedExposureInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.ExposureInheritance);
			_selectedContextInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.ContextInheritance);

			settings.PropertyChanged += ReadSettings_PropertyChanged;

			InitializePermissions();
			InitializeExposureMode();
			InitializeAgentFilter();

			SelectAllAgentsCommand = new RelayCommand(() => SetAllAgentsFilter(true));
			DeselectAllAgentsCommand = new RelayCommand(() => SetAllAgentsFilter(false));
		}

		private void ReadSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(AgentReadSettings.ReadingInheritance):
					_selectedReadingInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == ReadSettings.ReadingInheritance);
					RaisePropertyChanged(nameof(SelectedReadingInheritance));
					RaisePropertyChanged(nameof(EffectiveReading));
					InitializePermissions();
					break;

				case nameof(AgentReadSettings.ExposureInheritance):
					_selectedExposureInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == ReadSettings.ExposureInheritance);
					RaisePropertyChanged(nameof(SelectedExposureInheritance));
					RaisePropertyChanged(nameof(EffectiveExposure));
					InitializeExposureMode();
					break;

				case nameof(AgentReadSettings.ContextInheritance):
					_selectedContextInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == ReadSettings.ContextInheritance);
					RaisePropertyChanged(nameof(SelectedContextInheritance));
					RaisePropertyChanged(nameof(EffectiveContext));
					break;
			}
		}

		private void InitializePermissions()
		{
			ReadPermissionItems.Clear();

			var perms = EffectiveReading.ReadPermissions;

			ReadPermissionItems.Add(new ReadPermissionItem(this, AgentReadPermissions.UserMessages,
				LocalizationManager.LocalizeStatic("perm_user_messages"),
				LocalizationManager.LocalizeStatic("perm_user_messages_hint"),
				perms.HasFlag(AgentReadPermissions.UserMessages)));

			ReadPermissionItems.Add(new ReadPermissionItem(this, AgentReadPermissions.UserAttachments,
				LocalizationManager.LocalizeStatic("perm_user_attachments"),
				LocalizationManager.LocalizeStatic("perm_user_attachments_hint"),
				perms.HasFlag(AgentReadPermissions.UserAttachments)));

			ReadPermissionItems.Add(new ReadPermissionItem(this, AgentReadPermissions.OwnMessages,
				LocalizationManager.LocalizeStatic("perm_own_messages"),
				LocalizationManager.LocalizeStatic("perm_own_messages_hint"),
				perms.HasFlag(AgentReadPermissions.OwnMessages)));

			ReadPermissionItems.Add(new ReadPermissionItem(this, AgentReadPermissions.OtherAgentMessages,
				LocalizationManager.LocalizeStatic("perm_other_agent_messages"),
				LocalizationManager.LocalizeStatic("perm_other_agent_messages_hint"),
				perms.HasFlag(AgentReadPermissions.OtherAgentMessages)));

			ReadPermissionItems.Add(new ReadPermissionItem(this, AgentReadPermissions.OtherAgentContent,
				LocalizationManager.LocalizeStatic("perm_other_agent_content"),
				LocalizationManager.LocalizeStatic("perm_other_agent_content_hint"),
				perms.HasFlag(AgentReadPermissions.OtherAgentContent)));

			ReadPermissionItems.Add(new ReadPermissionItem(this, AgentReadPermissions.OtherAgentReasoning,
				LocalizationManager.LocalizeStatic("perm_other_agent_reasoning"),
				LocalizationManager.LocalizeStatic("perm_other_agent_reasoning_hint"),
				perms.HasFlag(AgentReadPermissions.OtherAgentReasoning)));

			ReadPermissionItems.Add(new ReadPermissionItem(this, AgentReadPermissions.OtherAgentToolCalls,
				LocalizationManager.LocalizeStatic("perm_other_agent_tool_calls"),
				LocalizationManager.LocalizeStatic("perm_other_agent_tool_calls_hint"),
				perms.HasFlag(AgentReadPermissions.OtherAgentToolCalls)));

			ReadPermissionItems.Add(new ReadPermissionItem(this, AgentReadPermissions.OtherAgentAttachments,
				LocalizationManager.LocalizeStatic("perm_other_agent_attachments"),
				LocalizationManager.LocalizeStatic("perm_other_agent_attachments_hint"),
				perms.HasFlag(AgentReadPermissions.OtherAgentAttachments)));

			ReadPermissionItems.Add(new ReadPermissionItem(this, AgentReadPermissions.MessagesWithToolCalls,
				LocalizationManager.LocalizeStatic("perm_messages_with_tool_calls"),
				LocalizationManager.LocalizeStatic("perm_messages_with_tool_calls_hint"),
				perms.HasFlag(AgentReadPermissions.MessagesWithToolCalls)));

			ReadPermissionItems.Add(new ReadPermissionItem(this, AgentReadPermissions.IdentifyAgentsAsUsers,
				LocalizationManager.LocalizeStatic("perm_identify_agents_as_users"),
				LocalizationManager.LocalizeStatic("perm_identify_agents_as_users_hint"),
				perms.HasFlag(AgentReadPermissions.IdentifyAgentsAsUsers)));
		}

		private void InitializeExposureMode()
		{
			ExposureModeItems.Clear();

			var mode = EffectiveExposure.ExposureMode;

			ExposureModeItems.Add(new ExposureModeItem(this, AgentExposureMode.Content,
				LocalizationManager.LocalizeStatic("exposure_content"),
				LocalizationManager.LocalizeStatic("exposure_content_hint"),
				mode.HasFlag(AgentExposureMode.Content)));

			ExposureModeItems.Add(new ExposureModeItem(this, AgentExposureMode.Reasoning,
				LocalizationManager.LocalizeStatic("exposure_reasoning"),
				LocalizationManager.LocalizeStatic("exposure_reasoning_hint"),
				mode.HasFlag(AgentExposureMode.Reasoning)));

			ExposureModeItems.Add(new ExposureModeItem(this, AgentExposureMode.ToolCalls,
				LocalizationManager.LocalizeStatic("exposure_tool_calls"),
				LocalizationManager.LocalizeStatic("exposure_tool_calls_hint"),
				mode.HasFlag(AgentExposureMode.ToolCalls)));

			ExposureModeItems.Add(new ExposureModeItem(this, AgentExposureMode.Attachments,
				LocalizationManager.LocalizeStatic("exposure_attachments"),
				LocalizationManager.LocalizeStatic("exposure_attachments_hint"),
				mode.HasFlag(AgentExposureMode.Attachments)));

			ExposureModeItems.Add(new ExposureModeItem(this, AgentExposureMode.MessagesWithToolCalls,
				LocalizationManager.LocalizeStatic("exposure_messages_with_tool_calls"),
				LocalizationManager.LocalizeStatic("exposure_messages_with_tool_calls_hint"),
				mode.HasFlag(AgentExposureMode.MessagesWithToolCalls)));

			ExposureModeItems.Add(new ExposureModeItem(this, AgentExposureMode.IdentifySelfAsUser,
				LocalizationManager.LocalizeStatic("exposure_identify_self_as_user"),
				LocalizationManager.LocalizeStatic("exposure_identify_self_as_user_hint"),
				mode.HasFlag(AgentExposureMode.IdentifySelfAsUser)));
		}

		private void InitializeAgentFilter()
		{
			AgentFilterItems.Clear();

			// Get global agents
			var globalConfig = SettingsManager.Get<AgentsConfiguration>();

			// Combine global + chat-local agents, deduplicate by ID
			var allAgents = new List<(ChatAgentDescriptor Descriptor, bool IsGlobal)>();

			foreach (var agent in globalConfig.Agents)
				allAgents.Add((agent, true));

			foreach (var agent in _chatAgents)
				if (!allAgents.Any(a => a.Descriptor.Id == agent.Id))
					allAgents.Add((agent, false));

			foreach (var (descriptor, isGlobal) in allAgents)
			{
				if (descriptor.Id == _agentId) continue;

				bool isSelected = ReadSettings.AgentIdsReadFilter.Contains(descriptor.Id);
				AgentFilterItems.Add(new AgentFilterItem(this, descriptor, isGlobal, isSelected));
			}
		}

		public void UpdateAgentFilter()
		{
			ReadSettings.AgentIdsReadFilter.Clear();
			foreach (var item in AgentFilterItems)
			{
				if (item.IsSelected)
					ReadSettings.AgentIdsReadFilter.Add(item.Agent.Id);
			}

			RaisePropertyChanged(nameof(HasFilter));
			RaisePropertyChanged(nameof(IsWhitelistWithSelection));
			RaisePropertyChanged(nameof(IsBlacklistWithSelection));
		}

		private void SetAllAgentsFilter(bool selected)
		{
			foreach (var item in AgentFilterItems)
				item.IsSelected = selected;
		}
	}
}
