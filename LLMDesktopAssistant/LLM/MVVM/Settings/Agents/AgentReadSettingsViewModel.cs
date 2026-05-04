using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Settings;
using System.Collections.ObjectModel;
using System.Windows.Input;

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
					_parent.ReadSettings.ReadPermissions = (_parent.ReadSettings.ReadPermissions & ~Permission) | (value ? Permission : 0);
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
					_parent.ReadSettings.ExposureMode = (_parent.ReadSettings.ExposureMode & ~Mode) | (value ? Mode : 0);
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
		public AgentDescriptor Agent { get; }
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

		public AgentFilterItem(AgentReadSettingsViewModel parent, AgentDescriptor agent, bool isGlobal, bool isSelected)
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
		private readonly ICollection<AgentDescriptor> _chatAgents;
		private readonly Guid _agentId;

		public AgentReadSettings ReadSettings { get; }

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

		public AgentReadSettingsViewModel(AgentReadSettings settings,
			ICollection<AgentDescriptor> chatAgents, Guid agentId)
		{
			ReadSettings = settings;
			_chatAgents = chatAgents;
			_agentId = agentId;

			InitializePermissions();
			InitializeExposureMode();
			InitializeAgentFilter();

			SelectAllAgentsCommand = new RelayCommand(() => SetAllAgentsFilter(true));
			DeselectAllAgentsCommand = new RelayCommand(() => SetAllAgentsFilter(false));
		}

		private void InitializePermissions()
		{
			ReadPermissionItems.Clear();

			var perms = ReadSettings.ReadPermissions;

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

			var mode = ReadSettings.ExposureMode;

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
			var allAgents = new List<(AgentDescriptor Descriptor, bool IsGlobal)>();

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
