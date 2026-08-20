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
					_parent.SetReadPermission(Permission, value);
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
					_parent.SetExposureMode(Mode, value);
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
		/// Gets the effective read permissions resolved by the current inheritance level.
		/// </summary>
		public AgentReadPermissions EffectiveReadPermissions => ReadSettings.GetEffectiveReadPermissions(_chatSettings);

		/// <summary>
		/// Gets the effective exposure mode resolved by the current inheritance level.
		/// </summary>
		public AgentExposureMode EffectiveExposureMode => ReadSettings.GetEffectiveExposureMode(_chatSettings);

		/// <summary>
		/// Gets the effective context group resolved by the current inheritance level.
		/// </summary>
		public AgentContextSettings EffectiveContext => ReadSettings.GetEffectiveContext(_chatSettings);

		private InheritanceLevelItem _selectedReadPermissionsInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the read permissions.
		/// </summary>
		public InheritanceLevelItem SelectedReadPermissionsInheritance
		{
			get => _selectedReadPermissionsInheritance;
			set
			{
				if (SetProperty(ref _selectedReadPermissionsInheritance, value) && value != null)
					ReadSettings.ReadPermissionsInheritance = value.Value;
			}
		}

		private InheritanceLevelItem _selectedExposureModeInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the exposure mode.
		/// </summary>
		public InheritanceLevelItem SelectedExposureModeInheritance
		{
			get => _selectedExposureModeInheritance;
			set
			{
				if (SetProperty(ref _selectedExposureModeInheritance, value) && value != null)
					ReadSettings.ExposureModeInheritance = value.Value;
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

			_selectedReadPermissionsInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.ReadPermissionsInheritance);
			_selectedExposureModeInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == settings.ExposureModeInheritance);
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
				case nameof(AgentReadSettings.ReadPermissionsInheritance):
					_selectedReadPermissionsInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == ReadSettings.ReadPermissionsInheritance);
					RaisePropertyChanged(nameof(SelectedReadPermissionsInheritance));
					RaisePropertyChanged(nameof(EffectiveReadPermissions));
					InitializePermissions();
					break;

				case nameof(AgentReadSettings.ExposureModeInheritance):
					_selectedExposureModeInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == ReadSettings.ExposureModeInheritance);
					RaisePropertyChanged(nameof(SelectedExposureModeInheritance));
					RaisePropertyChanged(nameof(EffectiveExposureMode));
					InitializeExposureMode();
					break;

				case nameof(AgentReadSettings.ContextInheritance):
					_selectedContextInheritance = InheritanceLevelItem.AllAgent.First(i => i.Value == ReadSettings.ContextInheritance);
					RaisePropertyChanged(nameof(SelectedContextInheritance));
					RaisePropertyChanged(nameof(EffectiveContext));
					break;
			}
		}

		internal void SetReadPermission(AgentReadPermissions permission, bool enabled)
			=> ReadSettings.SetEffectiveReadPermissions(_chatSettings, (EffectiveReadPermissions & ~permission) | (enabled ? permission : 0));

		internal void SetExposureMode(AgentExposureMode mode, bool enabled)
			=> ReadSettings.SetEffectiveExposureMode(_chatSettings, (EffectiveExposureMode & ~mode) | (enabled ? mode : 0));

		private void InitializePermissions()
		{
			ReadPermissionItems.Clear();

			var perms = EffectiveReadPermissions;

			ReadPermissionItems.Add(new ReadPermissionItem(this, AgentReadPermissions.UserMessages,
				LocalizationManager.LocalizeStatic("agent.perm.user_messages"),
				LocalizationManager.LocalizeStatic("agent.perm.user_messages.hint"),
				perms.HasFlag(AgentReadPermissions.UserMessages)));

			ReadPermissionItems.Add(new ReadPermissionItem(this, AgentReadPermissions.UserAttachments,
				LocalizationManager.LocalizeStatic("agent.perm.user_attachments"),
				LocalizationManager.LocalizeStatic("agent.perm.user_attachments.hint"),
				perms.HasFlag(AgentReadPermissions.UserAttachments)));

			ReadPermissionItems.Add(new ReadPermissionItem(this, AgentReadPermissions.OwnMessages,
				LocalizationManager.LocalizeStatic("agent.perm.own_messages"),
				LocalizationManager.LocalizeStatic("agent.perm.own_messages.hint"),
				perms.HasFlag(AgentReadPermissions.OwnMessages)));

			ReadPermissionItems.Add(new ReadPermissionItem(this, AgentReadPermissions.OtherAgentMessages,
				LocalizationManager.LocalizeStatic("agent.perm.other_agent_messages"),
				LocalizationManager.LocalizeStatic("agent.perm.other_agent_messages.hint"),
				perms.HasFlag(AgentReadPermissions.OtherAgentMessages)));

			ReadPermissionItems.Add(new ReadPermissionItem(this, AgentReadPermissions.OtherAgentContent,
				LocalizationManager.LocalizeStatic("agent.perm.other_agent_content"),
				LocalizationManager.LocalizeStatic("agent.perm.other_agent_content.hint"),
				perms.HasFlag(AgentReadPermissions.OtherAgentContent)));

			ReadPermissionItems.Add(new ReadPermissionItem(this, AgentReadPermissions.OtherAgentReasoning,
				LocalizationManager.LocalizeStatic("agent.perm.other_agent_reasoning"),
				LocalizationManager.LocalizeStatic("agent.perm.other_agent_reasoning.hint"),
				perms.HasFlag(AgentReadPermissions.OtherAgentReasoning)));

			ReadPermissionItems.Add(new ReadPermissionItem(this, AgentReadPermissions.OtherAgentToolCalls,
				LocalizationManager.LocalizeStatic("agent.perm.other_agent_tool_calls"),
				LocalizationManager.LocalizeStatic("agent.perm.other_agent_tool_calls.hint"),
				perms.HasFlag(AgentReadPermissions.OtherAgentToolCalls)));

			ReadPermissionItems.Add(new ReadPermissionItem(this, AgentReadPermissions.OtherAgentAttachments,
				LocalizationManager.LocalizeStatic("agent.perm.other_agent_attachments"),
				LocalizationManager.LocalizeStatic("agent.perm.other_agent_attachments.hint"),
				perms.HasFlag(AgentReadPermissions.OtherAgentAttachments)));

			ReadPermissionItems.Add(new ReadPermissionItem(this, AgentReadPermissions.MessagesWithToolCalls,
				LocalizationManager.LocalizeStatic("agent.perm.messages_with_tool_calls"),
				LocalizationManager.LocalizeStatic("agent.perm.messages_with_tool_calls.hint"),
				perms.HasFlag(AgentReadPermissions.MessagesWithToolCalls)));

			ReadPermissionItems.Add(new ReadPermissionItem(this, AgentReadPermissions.IdentifyAgentsAsUsers,
				LocalizationManager.LocalizeStatic("agent.perm.identify_agents_as_users"),
				LocalizationManager.LocalizeStatic("agent.perm.identify_agents_as_users.hint"),
				perms.HasFlag(AgentReadPermissions.IdentifyAgentsAsUsers)));
		}

		private void InitializeExposureMode()
		{
			ExposureModeItems.Clear();

			var mode = EffectiveExposureMode;

			ExposureModeItems.Add(new ExposureModeItem(this, AgentExposureMode.Content,
				LocalizationManager.LocalizeStatic("agent.exposure.content"),
				LocalizationManager.LocalizeStatic("agent.exposure.content.hint"),
				mode.HasFlag(AgentExposureMode.Content)));

			ExposureModeItems.Add(new ExposureModeItem(this, AgentExposureMode.Reasoning,
				LocalizationManager.LocalizeStatic("agent.exposure.reasoning"),
				LocalizationManager.LocalizeStatic("agent.exposure.reasoning.hint"),
				mode.HasFlag(AgentExposureMode.Reasoning)));

			ExposureModeItems.Add(new ExposureModeItem(this, AgentExposureMode.ToolCalls,
				LocalizationManager.LocalizeStatic("agent.exposure.tool_calls"),
				LocalizationManager.LocalizeStatic("agent.exposure.tool_calls.hint"),
				mode.HasFlag(AgentExposureMode.ToolCalls)));

			ExposureModeItems.Add(new ExposureModeItem(this, AgentExposureMode.Attachments,
				LocalizationManager.LocalizeStatic("agent.exposure.attachments"),
				LocalizationManager.LocalizeStatic("agent.exposure.attachments.hint"),
				mode.HasFlag(AgentExposureMode.Attachments)));

			ExposureModeItems.Add(new ExposureModeItem(this, AgentExposureMode.MessagesWithToolCalls,
				LocalizationManager.LocalizeStatic("agent.exposure.messages_with_tool_calls"),
				LocalizationManager.LocalizeStatic("agent.exposure.messages_with_tool_calls.hint"),
				mode.HasFlag(AgentExposureMode.MessagesWithToolCalls)));
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

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
				ReadSettings.PropertyChanged -= ReadSettings_PropertyChanged;
		}
	}
}
