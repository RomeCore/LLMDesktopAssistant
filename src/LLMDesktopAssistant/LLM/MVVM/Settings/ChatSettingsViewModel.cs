using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.ApiKeys;
using LLMDesktopAssistant.Data.Connectors;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM.Settings;
using LLMDesktopAssistant.LLM.MVVM.Settings.Agents;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.LLM.Services.Prompting;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Providers;
using LLMDesktopAssistant.Utils;
using Material.Icons;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Tools.Meta;
using LLMDesktopAssistant.Agents.Memory;
using LLMDesktopAssistant.Settings;

namespace LLMDesktopAssistant.LLM.Settings
{


	/// <summary>
	/// A settings tree node that represents an agent and groups its category nodes together.
	/// </summary>
	public class SettingsAgentParentNode : SettingsTreeNode
	{
		/// <summary>
		/// Gets the agent information shown on the node header.
		/// </summary>
		public AgentInformation Info { get; }

		/// <summary>
		/// Gets a value indicating whether the agent is global (shared between chats).
		/// </summary>
		public bool IsGlobal { get; }

		/// <summary>
		/// Gets the display name of the node.
		/// </summary>
		public override string DisplayName { get; }

		/// <summary>
		/// Gets the icon shown next to the node.
		/// </summary>
		public override MaterialIconKind Icon { get; }

		/// <summary>
		/// Gets the child nodes.
		/// </summary>
		public override IEnumerable<SettingsTreeNode> Children { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SettingsAgentParentNode"/> class.
		/// </summary>
		/// <param name="info">The agent information.</param>
		/// <param name="isGlobal">Whether the agent is global (shared between chats).</param>
		/// <param name="children">The child nodes.</param>
		public SettingsAgentParentNode(AgentInformation info, bool isGlobal, List<SettingsTreeNode> children)
			: base(() => new AgentInfoSettingsViewModel(info))
		{
			Info = info;
			IsGlobal = isGlobal;
			DisplayName = info.Name;
			Icon = MaterialIconKind.Robot;
			Children = children;
		}
	}


	/// <summary>
	/// ViewModel for the chat settings dialog. Builds a tree of settings categories whose
	/// view models are created lazily when the category is opened and disposed when the
	/// selection changes or the dialog is closed.
	/// </summary>
	[ViewModelFor(typeof(ChatSettingsView))]
	public class ChatSettingsViewModel : ViewModelBase
	{
		private readonly IAgentManagementService _agentManager;
		private int _generalSettingsCount;

		/// <summary>
		/// Gets the chat settings being edited.
		/// </summary>
		public ChatSettings Settings { get; }

		/// <summary>
		/// Gets the chat the settings belong to.
		/// </summary>
		public Chat Chat { get; }

		/// <summary>
		/// Gets the tree of settings categories.
		/// </summary>
		public RangeObservableCollection<SettingsTreeNode> SettingsTree { get; } = [];

		private SettingsTreeNode? _selectedNode;
		/// <summary>
		/// Gets or sets the currently selected settings tree node. Selecting a node creates
		/// its view model; selecting another node releases (disposes) the previous one.
		/// </summary>
		public SettingsTreeNode? SelectedNode
		{
			get => _selectedNode;
			set
			{
				if (ReferenceEquals(_selectedNode, value))
					return;

				if (_selectedNode is not null && _selectedNode.ReleaseViewModel() is ChatAgentsSettingsViewModel oldAgents)
					oldAgents.AgentsChanged -= OnAgentsChanged;

				if (SetProperty(ref _selectedNode, value))
				{
					if (_selectedNode?.ViewModel is ChatAgentsSettingsViewModel newAgents)
						newAgents.AgentsChanged += OnAgentsChanged;
				}
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatSettingsViewModel"/> class.
		/// </summary>
		/// <param name="settings">The chat settings to edit.</param>
		/// <param name="chat">The chat the settings belong to.</param>
		public ChatSettingsViewModel(ChatSettings settings, Chat chat)
		{
			Settings = settings;
			Chat = chat;
			_agentManager = chat.Services.GetRequiredService<IAgentManagementService>();

			InitializeTree();
		}

		private void InitializeTree()
		{
			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings.chat.users"),
				MaterialIconKind.AccountCircle,
				() => new ChatUserSettingsViewModel(Settings.Users.Users)));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings.agents"),
				MaterialIconKind.Robot,
				() => new ChatAgentsSettingsViewModel(Settings.Agents, _agentManager)));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings.execution_stages"),
				MaterialIconKind.RobotConfused,
				() => new ChatExecutionStagesSettingsViewModel(Settings.Agents, _agentManager)));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings.chat.models"),
				MaterialIconKind.Brain,
				() => new ChatModelSettingsViewModel(Settings.Models)));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings.chat.environment"),
				MaterialIconKind.FolderSettings,
				() => new ChatEnvironmentSettingsViewModel(Settings.Environment,
					Chat.Services.GetServices<IScriptEngineEnvConfigurationProvider>(), Chat.Services.GetService<IExplorerOpener>())));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings.chat.databases"),
				MaterialIconKind.DatabaseSearch,
				() => new ChatDatabaseSettingsViewModel(Settings.Databases,
					Chat.Services.GetRequiredService<IApiKeyManagerService>(),
					Chat.Services.GetRequiredService<IDatabaseConnectionCache>(),
					Chat.Services.GetRequiredService<IDatabaseConnectionManager>())));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings.chat.mcp"),
				MaterialIconKind.Connection,
				() => new ChatMCPSettingsViewModel(Settings.Mcp, Chat.Services.GetRequiredService<IMCPManagementService>())));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings.chat.tools"),
				MaterialIconKind.Wrench,
				() => new ChatToolsSettingsViewModel(Settings.Tools,
					Chat.Services.GetRequiredService<IMetaToolManagementService>(),
					Chat.Services.GetServices<IMetaToolEngine>())));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings.chat.skills"),
				MaterialIconKind.Cards,
				() => new ChatSkillsSettingsViewModel(Settings.Skills,
					Chat.Services.GetRequiredService<ISkillsetBuildingService>())));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings.chat.sub_agents"),
				MaterialIconKind.RobotHappy,
				() => new ChatSubAgentsSettingsViewModel(Settings.SubAgents,
					Chat.Services.GetRequiredService<ISubAgentSetBuildingService>(),
					Chat.Services.GetRequiredService<ISkillsetBuildingService>())));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings.chat.memory"),
				MaterialIconKind.Database,
				() => new ChatMemorySettingsViewModel(Settings.Memory)));

			_generalSettingsCount = SettingsTree.Count;

			RebuildAgents();
		}

		/// <summary>
		/// Rebuilds the agent subtree from the current agent list, releasing the view models
		/// of the removed nodes.
		/// </summary>
		public void RebuildAgents()
		{
			if (SettingsTree.Count > _generalSettingsCount)
			{
				for (int i = SettingsTree.Count - 1; i >= _generalSettingsCount; i--)
				{
					var removed = SettingsTree[i];
					if (IsInSubtree(_selectedNode, removed))
					{
						_selectedNode?.ReleaseViewModel();
						_selectedNode = null;
						RaisePropertyChanged(nameof(SelectedNode));
					}
					removed.Dispose();
				}

				SettingsTree.RemoveRange(_generalSettingsCount, SettingsTree.Count - _generalSettingsCount);
			}

			var promptRegistry = Chat.Services.GetRequiredService<IPromptRegistry>();

			var allAgents = _agentManager.ListAgents();
			foreach (var (descriptor, isGlobal) in allAgents)
			{
				var agentChildren = new List<SettingsTreeNode>
				{
					new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings.chat.execution"),
						MaterialIconKind.Play,
						() => new AgentExecutionConditionsSettingsViewModel(descriptor.ExecutionConditions, Settings)),

					new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings.chat.llm_properties"),
						MaterialIconKind.Tune,
						() => new AgentGenerationSettingsViewModel(descriptor.Generation, Settings)),

					new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings.chat.read"),
						MaterialIconKind.Eye,
						() => new AgentReadSettingsViewModel(
							descriptor.Read, Settings.Agents.ChatAgents, descriptor.Id, Settings)),

					new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings.chat.prompts"),
						MaterialIconKind.Text,
						() => new AgentPromptSettingsViewModel(
							descriptor.Prompts,
							Settings,
							promptRegistry,
							Chat.Services.GetRequiredService<IChatPromptBuilder>(),
							descriptor)),

					new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings.chat.tools"),
						MaterialIconKind.Wrench,
						() => new AgentToolSettingsViewModel(
							descriptor.Tools,
							Chat.Services.GetRequiredService<IToolsetBuildingService>(),
							Settings)),
					new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings.chat.skills"),
						MaterialIconKind.Cards,
						() => new AgentSkillSettingsViewModel(
							descriptor.Skills,
							Chat.Services.GetRequiredService<ISkillsetBuildingService>(),
							Settings)),

					new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings.chat.sub_agents"),
						MaterialIconKind.RobotHappy,
						() => new AgentSubAgentSettingsViewModel(
							descriptor.SubAgents,
							Settings,
							Chat.Services.GetRequiredService<ISubAgentSetBuildingService>(),
							Chat.Services.GetRequiredService<ISkillsetBuildingService>())),

					new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings.chat.memory"),
						MaterialIconKind.Database,
						() => new AgentMemorySettingsViewModel(
							descriptor.Memory,
							Settings,
							Chat.Services.GetRequiredService<IMemoryDatabaseManager>(),
							Chat.Services.GetRequiredService<IMemoryFactStore>(),
							Chat.Services.GetRequiredService<IMemoryLogStore>())),
				};

				SettingsTree.Add(new SettingsAgentParentNode(descriptor.Info, isGlobal, agentChildren));
			}
		}

		private static bool IsInSubtree(SettingsTreeNode? node, SettingsTreeNode subtreeRoot)
		{
			if (node is null)
				return false;
			if (ReferenceEquals(node, subtreeRoot))
				return true;
			return subtreeRoot.Children?.Any(c => IsInSubtree(node, c)) ?? false;
		}

		private void OnAgentsChanged()
		{
			RebuildAgents();
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				foreach (var node in SettingsTree)
					node.Dispose();
				SettingsTree.Clear();
			}
		}
	}
}
