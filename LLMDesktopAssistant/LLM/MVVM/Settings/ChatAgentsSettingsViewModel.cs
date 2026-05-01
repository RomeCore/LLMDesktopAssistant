using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings
{
	// TODO: Тут дикий нейрослоп, нужно фиксить

	/// <summary>
	/// ViewModel for the Agents management tab in chat settings.
	/// Handles listing, adding, removing, activating/deactivating, reordering,
	/// and promoting/demoting agents between global and local scopes.
	/// </summary>
	[ViewModelFor(typeof(ChatAgentsSettingsView))]
	public class ChatAgentsSettingsViewModel : ViewModelBase
	{
		public ChatAgentSettings AgentSettings { get; }
		public IAgentManagementService AgentManager { get; }

		public RangeObservableCollection<AgentOptionViewModel> AgentOptions { get; } = [];

		private AgentOptionViewModel? _selectedAgent;
		public AgentOptionViewModel? SelectedAgent
		{
			get => _selectedAgent;
			set => SetProperty(ref _selectedAgent, value);
		}

		public IRelayCommand AddAgentCommand { get; }
		public IRelayCommand RemoveAgentCommand { get; }
		public IRelayCommand<AgentOptionViewModel> ToggleAgentActiveCommand { get; }
		public IRelayCommand<AgentOptionViewModel> MoveAgentUpCommand { get; }
		public IRelayCommand<AgentOptionViewModel> MoveAgentDownCommand { get; }
		public IRelayCommand<AgentOptionViewModel> PromoteToGlobalCommand { get; }
		public IRelayCommand<AgentOptionViewModel> CopyToLocalCommand { get; }

		/// <summary>
		/// Raised when the agent list changes (promote/demote) so parent VM can rebuild tree.
		/// </summary>
		public event Action? AgentsChanged;

		public ChatAgentsSettingsViewModel(ChatAgentSettings agentSettings, IAgentManagementService agentManager)
		{
			AgentSettings = agentSettings;
			AgentManager = agentManager;

			AddAgentCommand = new RelayCommand(AddAgent);
			RemoveAgentCommand = new RelayCommand(RemoveSelectedAgent);
			ToggleAgentActiveCommand = new RelayCommand<AgentOptionViewModel>(ToggleAgentActive);
			MoveAgentUpCommand = new RelayCommand<AgentOptionViewModel>(MoveAgentUp, CanMoveAgentUp);
			MoveAgentDownCommand = new RelayCommand<AgentOptionViewModel>(MoveAgentDown, CanMoveAgentDown);
			PromoteToGlobalCommand = new RelayCommand<AgentOptionViewModel>(PromoteToGlobal);
			CopyToLocalCommand = new RelayCommand<AgentOptionViewModel>(CopyToLocal, CanCopyToLocal);

			AgentSettings.EnsureDefaultAgent();
			RefreshAgentList();
		}

		private void NotifyAgentsChanged()
		{
			AgentsChanged?.Invoke();
		}

		private bool IsAgentActive(AgentDescriptor agent)
		{
			return AgentSettings.ActiveAgents.Any(a => a.AgentId == agent.Id && a.Enabled);
		}

		private int GetAgentOrderIndex(AgentDescriptor agent)
		{
			for (int i = 0; i < AgentSettings.ActiveAgents.Count; i++)
				if (AgentSettings.ActiveAgents[i].AgentId == agent.Id)
					return i;
			return -1;
		}

		private void RefreshAgentList()
		{
			AgentOptions.Clear();
			var allAgents = AgentManager.ListAgents();

			var sorted = allAgents
				.Select(a => new
				{
					Descriptor = a.Agent,
					IsGlobal = a.IsGlobal,
					OrderIndex = GetAgentOrderIndex(a.Agent)
				})
				.OrderByDescending(a => a.OrderIndex >= 0)
				.ThenBy(a => a.OrderIndex >= 0 ? a.OrderIndex : int.MaxValue)
				.ThenBy(a => a.IsGlobal ? 0 : 1)
				.ToList();

			foreach (var item in sorted)
			{
				AgentOptions.Add(new AgentOptionViewModel
				{
					Agent = item.Descriptor,
					IsGlobal = item.IsGlobal,
					IsActive = item.OrderIndex >= 0
				});
			}
		}

		private void AddAgent()
		{
			var newAgent = new AgentDescriptor();
			newAgent.Info.Name = $"Agent {AgentSettings.ChatAgents.Count + 1}";
			AgentSettings.ChatAgents.Add(newAgent);

			RefreshAgentList();
			NotifyAgentsChanged();
		}

		private void RemoveSelectedAgent()
		{
			if (SelectedAgent == null || SelectedAgent.IsGlobal)
				return;

			var agent = SelectedAgent.Agent;
			AgentSettings.ChatAgents.Remove(agent);

			for (int i = AgentSettings.ActiveAgents.Count - 1; i >= 0; i--)
			{
				if (AgentSettings.ActiveAgents[i].AgentId == agent.Id)
				{
					AgentSettings.ActiveAgents.RemoveAt(i);
					break;
				}
			}

			RefreshAgentList();
			NotifyAgentsChanged();
		}

		private void ToggleAgentActive(AgentOptionViewModel? option)
		{
			if (option == null) return;

			var existing = AgentSettings.ActiveAgents.FirstOrDefault(a => a.AgentId == option.Agent.Id);
			if (existing != null)
			{
				AgentSettings.ActiveAgents.Remove(existing);
				option.IsActive = false;
			}
			else
			{
				AgentSettings.ActiveAgents.Add(new AgentInstanceSettings
				{
					AgentId = option.Agent.Id,
					Enabled = true
				});
				option.IsActive = true;
			}
		}

		private void MoveAgentUp(AgentOptionViewModel? option)
		{
			if (option == null || !option.IsActive) return;

			var idx = GetAgentOrderIndex(option.Agent);
			if (idx <= 0) return;

			AgentSettings.ActiveAgents.Move(idx, idx - 1);
			RefreshAgentList();
		}
		private bool CanMoveAgentUp(AgentOptionViewModel? option)
		{
			if (option == null || !option.IsActive) return false;

			var idx = GetAgentOrderIndex(option.Agent);
			if (idx <= 0) return false;

			return true;
		}

		private void MoveAgentDown(AgentOptionViewModel? option)
		{
			if (option == null || !option.IsActive) return;

			var idx = GetAgentOrderIndex(option.Agent);
			if (idx < 0 || idx >= AgentSettings.ActiveAgents.Count - 1) return;

			AgentSettings.ActiveAgents.Move(idx, idx + 2); // idx + 2 will decrease to idx + 1 inside
			RefreshAgentList();
		}
		private bool CanMoveAgentDown(AgentOptionViewModel? option)
		{
			if (option == null || !option.IsActive) return false;

			var idx = GetAgentOrderIndex(option.Agent);
			if (idx < 0 || idx >= AgentSettings.ActiveAgents.Count - 1) return false;

			return true;
		}

		/// <summary>
		/// Promotes a local agent to global: copies it to global config and removes from local.
		/// </summary>
		private void PromoteToGlobal(AgentOptionViewModel? option)
		{
			if (option == null || option.IsGlobal)
				return;

			var agent = option.Agent;
			var globalConfig = SettingsManager.Get<AgentsConfiguration>();

			AgentSettings.ChatAgents.Remove(agent);
			globalConfig.Agents.Add(agent);

			for (int i = AgentSettings.ActiveAgents.Count - 1; i >= 0; i--)
			{
				if (AgentSettings.ActiveAgents[i].AgentId == agent.Id)
				{
					AgentSettings.ActiveAgents.RemoveAt(i);
					break;
				}
			}

			RefreshAgentList();
			NotifyAgentsChanged();
		}

		/// <summary>
		/// Copies a global agent to local chat agents (makes it available in this chat).
		/// </summary>
		private void CopyToLocal(AgentOptionViewModel? option)
		{
			if (option == null || !option.IsGlobal)
				return;
			var agent = option.Agent;
			if (AgentSettings.ChatAgents.Any(a => a.Id == agent.Id))
				return;

			// Add a copy of the global agent to local chat agents.
			var clone = agent.Clone();
			AgentSettings.ChatAgents.Add(clone);

			AgentSettings.ActiveAgents.Add(new AgentInstanceSettings
			{
				AgentId = clone.Id,
				Enabled = option.IsActive
			});

			RefreshAgentList();
			NotifyAgentsChanged();
		}
		private bool CanCopyToLocal(AgentOptionViewModel? option)
		{
			if (option == null || !option.IsGlobal)
				return false;
			var agent = option.Agent;
			if (AgentSettings.ChatAgents.Any(a => a.Id == agent.Id))
				return false;
			return true;
		}
	}

	public class AgentOptionViewModel : NotifyPropertyChanged
	{
		public required AgentDescriptor Agent { get; init; }
		public required bool IsGlobal { get; init; }

		private bool _isActive;
		public bool IsActive
		{
			get => _isActive;
			set => SetProperty(ref _isActive, value);
		}
	}
}
