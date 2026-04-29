using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings
{
	/// <summary>
	/// ViewModel for the Agents management tab in chat settings.
	/// Handles listing, adding, removing, activating/deactivating and reordering agents.
	/// </summary>
	[ViewModelFor(typeof(ChatAgentsSettingsView))]
	public class ChatAgentsSettingsViewModel : ViewModelBase
	{
		public ChatAgentSettings AgentSettings { get; }
		public Chat Chat { get; }

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

		public ChatAgentsSettingsViewModel(ChatAgentSettings agentSettings, Chat chat)
		{
			AgentSettings = agentSettings;
			Chat = chat;

			AddAgentCommand = new RelayCommand(AddAgent);
			RemoveAgentCommand = new RelayCommand(RemoveSelectedAgent);
			ToggleAgentActiveCommand = new RelayCommand<AgentOptionViewModel>(ToggleAgentActive);
			MoveAgentUpCommand = new RelayCommand<AgentOptionViewModel>(MoveAgentUp);
			MoveAgentDownCommand = new RelayCommand<AgentOptionViewModel>(MoveAgentDown);

			EnsureDefaultAgent();
			RefreshAgentList();
		}

		private void EnsureDefaultAgent()
		{
			var globalConfig = SettingsManager.Get<AgentsConfiguration>();
			if (globalConfig.Agents.Count > 0)
				return;

			if (AgentSettings.ChatAgents.Count == 0)
			{
				var defaultAgent = new AgentDescriptor();
				defaultAgent.Prompts.Nickname = "Default Assistant";
				AgentSettings.ChatAgents.Add(defaultAgent);
			}

			if (AgentSettings.ActiveAgents.Count == 0 && AgentSettings.ChatAgents.Count > 0)
			{
				AgentSettings.ActiveAgents.Add(new AgentInstanceSettings
				{
					AgentId = AgentSettings.ChatAgents[0].Id,
					Enabled = true
				});
			}
		}

		private List<(AgentDescriptor Descriptor, bool IsGlobal)> GetAllAgentsWithFlags()
		{
			var globalConfig = SettingsManager.Get<AgentsConfiguration>();
			var result = new List<(AgentDescriptor, bool)>();

			foreach (var agent in globalConfig.Agents)
				result.Add((agent, true));
			foreach (var agent in AgentSettings.ChatAgents)
				if (!result.Any(a => a.Item1.Id == agent.Id))
					result.Add((agent, false));

			return result;
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
			var allAgents = GetAllAgentsWithFlags();
			foreach (var (descriptor, isGlobal) in allAgents)
			{
				AgentOptions.Add(new AgentOptionViewModel
				{
					Agent = descriptor,
					DisplayName = descriptor.Prompts.Nickname ?? descriptor.Id.ToString()[..8],
					IsGlobal = isGlobal,
					IsActive = IsAgentActive(descriptor),
					OrderIndex = GetAgentOrderIndex(descriptor)
				});
			}
		}

		private void AddAgent()
		{
			var newAgent = new AgentDescriptor();
			newAgent.Prompts.Nickname = $"Agent {AgentSettings.ChatAgents.Count + 1}";
			AgentSettings.ChatAgents.Add(newAgent);

			RefreshAgentList();
		}

		private void RemoveSelectedAgent()
		{
			var selectedOption = AgentOptions.FirstOrDefault(o => o.IsSelected);
			if (selectedOption == null || selectedOption.IsGlobal)
				return;

			var agent = selectedOption.Agent;
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
		}

		private void ToggleAgentActive(AgentOptionViewModel? option)
		{
			if (option == null) return;

			var existing = AgentSettings.ActiveAgents.FirstOrDefault(a => a.AgentId == option.Agent.Id);
			if (existing != null)
			{
				AgentSettings.ActiveAgents.Remove(existing);
				option.IsActive = false;
				option.OrderIndex = -1;
			}
			else
			{
				AgentSettings.ActiveAgents.Add(new AgentInstanceSettings
				{
					AgentId = option.Agent.Id,
					Enabled = true
				});
				option.IsActive = true;
				option.OrderIndex = AgentSettings.ActiveAgents.Count - 1;
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

		private void MoveAgentDown(AgentOptionViewModel? option)
		{
			if (option == null || !option.IsActive) return;

			var idx = GetAgentOrderIndex(option.Agent);
			if (idx < 0 || idx >= AgentSettings.ActiveAgents.Count - 1) return;

			AgentSettings.ActiveAgents.Move(idx, idx + 1);
			RefreshAgentList();
		}
	}

	public class AgentOptionViewModel : NotifyPropertyChanged
	{
		public required AgentDescriptor Agent { get; init; }
		public required string DisplayName { get; init; }
		public required bool IsGlobal { get; init; }

		private bool _isActive;
		public bool IsActive
		{
			get => _isActive;
			set => SetProperty(ref _isActive, value);
		}

		private int _orderIndex = -1;
		public int OrderIndex
		{
			get => _orderIndex;
			set => SetProperty(ref _orderIndex, value);
		}

		private bool _isSelected;
		public bool IsSelected
		{
			get => _isSelected;
			set => SetProperty(ref _isSelected, value);
		}
	}
}
