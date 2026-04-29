using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Settings;
using System.Collections.ObjectModel;

namespace LLMDesktopAssistant.LLM.Settings
{
	[ViewModelFor(typeof(ChatSettingsView))]
	public class ChatSettingsViewModel : ViewModelBase
	{
		public ChatSettings Settings { get; }
		public Chat Chat { get; }

		public ChatModelSettingsViewModel ModelSettings { get; }
		public ChatEnvironmentSettingsViewModel EnvironmentSettings { get; }
		public ChatMCPSettingsViewModel McpSettings { get; }
		public ChatSummarizationSettingsViewModel SummarizationSettings { get; }

		public ObservableCollection<AgentOptionViewModel> AgentOptions { get; } = [];
		public AgentOptionViewModel? SelectedAgent
		{
			get => _selectedAgent;
			set
			{
				if (SetProperty(ref _selectedAgent, value))
					OnSelectedAgentChanged();
			}
		}
		private AgentOptionViewModel? _selectedAgent;

		private AgentDescriptor? _selectedAgentDescriptor;
		public AgentDescriptor? SelectedAgentDescriptor
		{
			get => _selectedAgentDescriptor;
			set => SetProperty(ref _selectedAgentDescriptor, value);
		}

		private ChatPromptSettingsViewModel? _agentPromptSettings;
		public ChatPromptSettingsViewModel? AgentPromptSettings
		{
			get => _agentPromptSettings;
			set => SetProperty(ref _agentPromptSettings, value);
		}

		private ChatToolSettingsViewModel? _agentToolSettings;
		public ChatToolSettingsViewModel? AgentToolSettings
		{
			get => _agentToolSettings;
			set => SetProperty(ref _agentToolSettings, value);
		}

		private ChatLLMPropertiesSettingsViewModel? _agentLLMPropertiesSettings;
		public ChatLLMPropertiesSettingsViewModel? AgentLLMPropertiesSettings
		{
			get => _agentLLMPropertiesSettings;
			set => SetProperty(ref _agentLLMPropertiesSettings, value);
		}

		public IRelayCommand AddAgentCommand { get; }
		public IRelayCommand RemoveAgentCommand { get; }

		public ChatSettingsViewModel(ChatSettings settings, Chat chat)
		{
			Settings = settings;
			Chat = chat;

			ModelSettings = new ChatModelSettingsViewModel(settings.Models);
			SummarizationSettings = new ChatSummarizationSettingsViewModel(settings.Summarization);
			EnvironmentSettings = new ChatEnvironmentSettingsViewModel(settings.Environment);
			McpSettings = new ChatMCPSettingsViewModel(settings.Mcp, chat.Services.GetRequiredService<IMCPManagementService>());

			AddAgentCommand = new RelayCommand(AddAgent);
			RemoveAgentCommand = new RelayCommand(RemoveSelectedAgent, () => SelectedAgent != null);

			RefreshAgentList();

			if (AgentOptions.Count > 0)
				SelectedAgent = AgentOptions[0];
		}

		private void RefreshAgentList()
		{
			AgentOptions.Clear();

			// Collect both global agents and chat-local agents
			var globalAgents = SettingsManager.Get<AgentsConfiguration>();

			var allAgents = new List<(AgentDescriptor Descriptor, bool IsGlobal)>();
			foreach (var agent in globalAgents.Agents)
				allAgents.Add((agent, true));
			foreach (var agent in Settings.Agents.ChatAgents)
				if (!allAgents.Any(a => a.Descriptor.Id == agent.Id))
					allAgents.Add((agent, false));

			foreach (var (descriptor, isGlobal) in allAgents)
			{
				AgentOptions.Add(new AgentOptionViewModel
				{
					Agent = descriptor,
					DisplayName = descriptor.Prompts.Nickname ?? descriptor.Id.ToString()[..8],
					IsGlobal = isGlobal
				});
			}
		}

		private void OnSelectedAgentChanged()
		{
			if (SelectedAgent == null)
			{
				SelectedAgentDescriptor = null;
				AgentPromptSettings = null;
				AgentToolSettings = null;
				AgentLLMPropertiesSettings = null;
				return;
			}

			SelectedAgentDescriptor = SelectedAgent.Agent;

			AgentPromptSettings = new ChatPromptSettingsViewModel(
				SelectedAgentDescriptor.Prompts
			);
			AgentToolSettings = new ChatToolSettingsViewModel(
				SelectedAgentDescriptor.Tools,
				Chat.Services.GetRequiredService<IToolsetBuildingService>()
			);
			AgentLLMPropertiesSettings = new ChatLLMPropertiesSettingsViewModel(
				SelectedAgentDescriptor.Generation
			);
		}

		private void AddAgent()
		{
			var newAgent = new AgentDescriptor();
			Settings.Agents.ChatAgents.Add(newAgent);

			RefreshAgentList();
			SelectedAgent = AgentOptions.LastOrDefault();
		}

		private void RemoveSelectedAgent()
		{
			if (SelectedAgent == null)
				return;

			var agent = SelectedAgent.Agent;
			Settings.Agents.ChatAgents.Remove(agent);

			for (int i = Settings.Agents.ActiveAgents.Count - 1; i >= 0; i--)
			{
				if (Settings.Agents.ActiveAgents[i].AgentId == agent.Id)
				{
					Settings.Agents.ActiveAgents.RemoveAt(i);
					break;
				}
			}

			RefreshAgentList();
			SelectedAgent = AgentOptions.FirstOrDefault();
		}
	}

	public class AgentOptionViewModel
	{
		public required AgentDescriptor Agent { get; init; }
		public required string DisplayName { get; init; }
		public required bool IsGlobal { get; init; }
	}
}
