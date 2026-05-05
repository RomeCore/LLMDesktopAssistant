using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.ExecutionStages;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings
{
	/// <summary>
	/// ViewModel for the Agent List tab in chat settings.
	/// Handles listing, adding, removing, and promoting/demoting agents between global and local scopes.
	/// Agent activation and ordering is now handled via Execution Stages.
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
		public IRelayCommand<AgentOptionViewModel> PromoteToGlobalCommand { get; }
		public IRelayCommand<AgentOptionViewModel> CopyToLocalCommand { get; }

		/// <summary>
		/// Raised when the agent list changes so parent VM can rebuild tree.
		/// </summary>
		public event Action? AgentsChanged;

		public ChatAgentsSettingsViewModel(ChatAgentSettings agentSettings, IAgentManagementService agentManager)
		{
			AgentSettings = agentSettings;
			AgentManager = agentManager;

			AddAgentCommand = new RelayCommand(AddAgent);
			RemoveAgentCommand = new RelayCommand(RemoveSelectedAgent);
			PromoteToGlobalCommand = new RelayCommand<AgentOptionViewModel>(PromoteToGlobal);
			CopyToLocalCommand = new RelayCommand<AgentOptionViewModel>(CopyToLocal, CanCopyToLocal);

			AgentSettings.EnsureDefaultAgent();
			RefreshAgentList();
		}

		private void NotifyAgentsChanged()
		{
			AgentsChanged?.Invoke();
		}

		private void RefreshAgentList()
		{
			AgentOptions.Clear();
			var allAgents = AgentManager.ListAgents();

			// Sort: local agents first, then global
			var sorted = allAgents
				.OrderBy(a => a.IsGlobal ? 1 : 0)
				.ThenBy(a => a.Agent.Info.Name)
				.ToList();

			foreach (var (agent, isGlobal) in sorted)
			{
				AgentOptions.Add(new AgentOptionViewModel
				{
					Agent = agent,
					IsGlobal = isGlobal
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

			// Also remove from all execution stages
			foreach (var stage in AgentSettings.ExecutionStages)
			{
				if (stage is SequentialAgentExecutionStage seq)
				{
					for (int i = seq.AgentInstances.Count - 1; i >= 0; i--)
					{
						if (seq.AgentInstances[i].AgentId == agent.Id)
							seq.AgentInstances.RemoveAt(i);
					}
				}
				else if (stage is RandomAgentExecutionStage rnd)
				{
					for (int i = rnd.AgentInstances.Count - 1; i >= 0; i--)
					{
						if (rnd.AgentInstances[i].AgentId == agent.Id)
							rnd.AgentInstances.RemoveAt(i);
					}
				}
			}

			RefreshAgentList();
			NotifyAgentsChanged();
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

			var clone = agent.Clone();
			AgentSettings.ChatAgents.Add(clone);

			RefreshAgentList();
			NotifyAgentsChanged();
		}

		private bool CanCopyToLocal(AgentOptionViewModel? option)
		{
			if (option == null || !option.IsGlobal)
				return false;
			return !AgentSettings.ChatAgents.Any(a => a.Id == option.Agent.Id);
		}
	}

	public class AgentOptionViewModel : NotifyPropertyChanged
	{
		public required AgentDescriptor Agent { get; init; }
		public required bool IsGlobal { get; init; }
	}
}
