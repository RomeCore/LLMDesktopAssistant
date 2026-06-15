using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM.Settings;
using LLMDesktopAssistant.LLM.MVVM.Settings.Agents;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Utils;
using Material.Icons;

namespace LLMDesktopAssistant.LLM.Settings
{
	public abstract class SettingsTreeNode : ViewModelBase
	{
		public abstract string DisplayName { get; }
		public abstract MaterialIconKind Icon { get; }
		public abstract IEnumerable<SettingsTreeNode>? Children { get; }
		public abstract object? ViewModel { get; }
	}

	public class SettingsParentNode : SettingsTreeNode
	{
		public override string DisplayName { get; }
		public override MaterialIconKind Icon { get; }
		public override IEnumerable<SettingsTreeNode> Children { get; }
		public override object? ViewModel { get; }

		public SettingsParentNode(string name, MaterialIconKind icon,
			List<SettingsTreeNode> children, object? viewModel)
		{
			DisplayName = name;
			Icon = icon;
			Children = children;
			ViewModel = viewModel;
		}
	}

	public class SettingsAgentParentNode : SettingsTreeNode
	{
		public AgentInformation Info { get; }
		public bool IsGlobal { get; }
		public override string DisplayName { get; }
		public override MaterialIconKind Icon { get; }
		public override IEnumerable<SettingsTreeNode> Children { get; }
		public override object? ViewModel { get; }

		public SettingsAgentParentNode(AgentInformation info, bool isGlobal, List<SettingsTreeNode> children)
		{
			Info = info;
			IsGlobal = isGlobal;
			DisplayName = info.Name;
			Icon = MaterialIconKind.Robot;
			Children = children;
			ViewModel = new AgentInfoSettingsViewModel(info);
		}
	}

	public class SettingsLeafNode : SettingsTreeNode
	{
		public override string DisplayName { get; }
		public override MaterialIconKind Icon { get; }
		public override IEnumerable<SettingsTreeNode>? Children => null;
		public override object? ViewModel { get; }

		public SettingsLeafNode(string name, MaterialIconKind icon, object? viewModel)
		{
			DisplayName = name;
			Icon = icon;
			ViewModel = viewModel;
		}
	}

	[ViewModelFor(typeof(ChatSettingsView))]
	public class ChatSettingsViewModel : ViewModelBase
	{
		public ChatSettings Settings { get; }
		public Chat Chat { get; }

		// Settings viewmodels
		public ChatModelSettingsViewModel ModelSettings { get; }
		public ChatEnvironmentSettingsViewModel EnvironmentSettings { get; }
		public ChatMCPSettingsViewModel McpSettings { get; }
		public ChatToolsSettingsViewModel ToolsSettings { get; }
		public ChatSummarizationSettingsViewModel SummarizationSettings { get; }
		public ChatAgentsSettingsViewModel AgentsSettings { get; }
		public ChatUserSettingsViewModel UserSettings { get; }

		public ChatExecutionStagesSettingsViewModel ExecutionStagesSettings { get; }

		private int _generalSettingsCount;
		public RangeObservableCollection<SettingsTreeNode> SettingsTree { get; } = [];

		private SettingsTreeNode? _selectedNode;
		public SettingsTreeNode? SelectedNode
		{
			get => _selectedNode;
			set => SetProperty(ref _selectedNode, value);
		}

		public ChatSettingsViewModel(ChatSettings settings, Chat chat)
		{
			Settings = settings;
			Chat = chat;

			var agentManager = chat.Services.GetRequiredService<IAgentManagementService>();

			AgentsSettings = new ChatAgentsSettingsViewModel(settings.Agents, agentManager);
			ExecutionStagesSettings = new ChatExecutionStagesSettingsViewModel(settings.Agents, agentManager);
			ModelSettings = new ChatModelSettingsViewModel(settings.Models);
			SummarizationSettings = new ChatSummarizationSettingsViewModel(settings.Summarization);
			UserSettings = new ChatUserSettingsViewModel(settings.Users.Users);

			EnvironmentSettings = new ChatEnvironmentSettingsViewModel(settings.Environment);
			McpSettings = new ChatMCPSettingsViewModel(settings.Mcp, chat.Services.GetRequiredService<IMCPManagementService>());

			ToolsSettings = new ChatToolsSettingsViewModel(settings.Tools);

			AgentsSettings.AgentsChanged += OnAgentsChanged;

			InitializeTree();
		}

		private void InitializeTree()
		{
			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("agents"),
				MaterialIconKind.Robot,
				AgentsSettings));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings_execution_stages"),
				MaterialIconKind.RobotConfused,
				ExecutionStagesSettings));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_models"),
				MaterialIconKind.Brain,
				ModelSettings));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_summarization"),
				MaterialIconKind.ArrowCollapseVertical,
				SummarizationSettings));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_users"),
				MaterialIconKind.AccountCircle,
				UserSettings));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_environment"),
				MaterialIconKind.FolderSettings,
				EnvironmentSettings));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_mcp"),
				MaterialIconKind.Connection,
				McpSettings));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_tools"),
				MaterialIconKind.Wrench,
				ToolsSettings));

			_generalSettingsCount = 8;

			RebuildAgents();
		}

		public void RebuildAgents()
		{
			if (SettingsTree.Count > _generalSettingsCount)
				SettingsTree.RemoveRange(_generalSettingsCount, SettingsTree.Count - _generalSettingsCount);

			var managementService = Chat.Services.GetRequiredService<IAgentManagementService>();
			var promptRegistry = Chat.Services.GetRequiredService<IPromptRegistry>();

			var allAgents = managementService.ListAgents();
			foreach (var (descriptor, isGlobal) in allAgents)
			{
				var agentChildren = new List<SettingsTreeNode>
				{
					new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_execution"),
						MaterialIconKind.Play,
						new AgentExecutionConditionsSettingsViewModel(descriptor.ExecutionConditions)),

					new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_llm_properties"),
						MaterialIconKind.Tune,
						new AgentGenerationSettingsViewModel(descriptor.Generation)),

					new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_read"),
						MaterialIconKind.Eye,
						new AgentReadSettingsViewModel(
							descriptor.Read, Settings.Agents.ChatAgents, descriptor.Id)),

					new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_prompts"),
						MaterialIconKind.Text,
						new AgentPromptSettingsViewModel(descriptor.Prompts, promptRegistry)),

					new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_tools"),
						MaterialIconKind.Wrench,
						new AgentToolSettingsViewModel(
							descriptor.Tools,
							Chat.Services.GetRequiredService<IToolsetBuildingService>())),
				};

				SettingsTree.Add(new SettingsAgentParentNode(descriptor.Info, isGlobal, agentChildren));
			}
		}

		private void OnAgentsChanged()
		{
			RebuildAgents();
		}
	}
}
