using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM.Settings;
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

		// Global settings viewmodels
		public ChatModelSettingsViewModel ModelSettings { get; }
		public ChatEnvironmentSettingsViewModel EnvironmentSettings { get; }
		public ChatMCPSettingsViewModel McpSettings { get; }
		public ChatSummarizationSettingsViewModel SummarizationSettings { get; }

		// Agents tab is in its own ViewModel
		public ChatAgentsSettingsViewModel AgentsSettings { get; }

		// --- Agent selector for Prompts/Tools/LLM properties tabs ---
		public ObservableCollection<AgentOptionViewModel> AgentSelectorOptions { get; } = [];
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

		private AgentReadSettingsViewModel? _agentReadSettings;
		public AgentReadSettingsViewModel? AgentReadSettings
		{
			get => _agentReadSettings;
			set => SetProperty(ref _agentReadSettings, value);
		}

		private AgentGenerationSettingsViewModel? _agentGenerationSettings;
		public AgentGenerationSettingsViewModel? AgentGenerationSettings
		{
			get => _agentGenerationSettings;
			set => SetProperty(ref _agentGenerationSettings, value);
		}

		private AgentPromptSettingsViewModel? _agentPromptSettings;
		public AgentPromptSettingsViewModel? AgentPromptSettings
		{
			get => _agentPromptSettings;
			set => SetProperty(ref _agentPromptSettings, value);
		}

		private AgentToolSettingsViewModel? _agentToolSettings;
		public AgentToolSettingsViewModel? AgentToolSettings
		{
			get => _agentToolSettings;
			set => SetProperty(ref _agentToolSettings, value);
		}

		public ChatSettingsViewModel(ChatSettings settings, Chat chat)
		{
			Settings = settings;
			Chat = chat;

			ModelSettings = new ChatModelSettingsViewModel(settings.Models);
			SummarizationSettings = new ChatSummarizationSettingsViewModel(settings.Summarization);
			EnvironmentSettings = new ChatEnvironmentSettingsViewModel(settings.Environment);
			McpSettings = new ChatMCPSettingsViewModel(settings.Mcp, chat.Services.GetRequiredService<IMCPManagementService>());

			AgentsSettings = new ChatAgentsSettingsViewModel(settings.Agents, chat);

			RefreshAgentSelector();

			if (AgentSelectorOptions.Count > 0)
				SelectedAgent = AgentSelectorOptions[0];
		}

		private List<(AgentDescriptor Descriptor, bool IsGlobal)> GetAllAgents()
		{
			var globalConfig = SettingsManager.Get<AgentsConfiguration>();
			var result = new List<(AgentDescriptor, bool)>();

			foreach (var agent in globalConfig.Agents)
				result.Add((agent, true));
			foreach (var agent in Settings.Agents.ChatAgents)
				if (!result.Any(a => a.Item1.Id == agent.Id))
					result.Add((agent, false));

			return result;
		}

		public void RefreshAgentSelector()
		{
			AgentSelectorOptions.Clear();
			var allAgents = GetAllAgents();
			foreach (var (descriptor, isGlobal) in allAgents)
			{
				AgentSelectorOptions.Add(new AgentOptionViewModel
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
				AgentReadSettings = null;
				AgentGenerationSettings = null;
				return;
			}

			SelectedAgentDescriptor = SelectedAgent.Agent;

			AgentPromptSettings = new AgentPromptSettingsViewModel(SelectedAgentDescriptor.Prompts);
			AgentToolSettings = new AgentToolSettingsViewModel(
				SelectedAgentDescriptor.Tools,
				Chat.Services.GetRequiredService<IToolsetBuildingService>()
			);
			AgentReadSettings = new AgentReadSettingsViewModel(SelectedAgentDescriptor.Read);
			AgentGenerationSettings = new AgentGenerationSettingsViewModel(SelectedAgentDescriptor.Generation);
		}
	}
}
