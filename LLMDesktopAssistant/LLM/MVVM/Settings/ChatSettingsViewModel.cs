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
using LLMDesktopAssistant.Utils;
using Material.Icons;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Agents.Memory;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// A node of the settings tree. The node view model is created lazily when the
	/// category is opened (selected) and released when the selection changes or the
	/// dialog is closed, so that category view models do not outlive their usage.
	/// </summary>
	public abstract class SettingsTreeNode : ViewModelBase
	{
		private readonly Func<object?> _viewModelFactory;
		private object? _viewModel;

		/// <summary>
		/// Initializes a new instance of the <see cref="SettingsTreeNode"/> class.
		/// </summary>
		/// <param name="viewModelFactory">The factory that creates the view model shown when this node is selected.</param>
		protected SettingsTreeNode(Func<object?> viewModelFactory)
		{
			_viewModelFactory = viewModelFactory;
		}

		/// <summary>
		/// Gets the display name of the node.
		/// </summary>
		public abstract string DisplayName { get; }

		/// <summary>
		/// Gets the icon shown next to the node.
		/// </summary>
		public abstract MaterialIconKind Icon { get; }

		/// <summary>
		/// Gets the child nodes, or <see langword="null"/> for leaf nodes.
		/// </summary>
		public abstract IEnumerable<SettingsTreeNode>? Children { get; }

		/// <summary>
		/// Gets the view model shown when this node is selected, creating it lazily on first access.
		/// </summary>
		public object? ViewModel => _viewModel ??= _viewModelFactory();

		/// <summary>
		/// Disposes the created view model (if any) and resets the cache so that the next
		/// access recreates it.
		/// </summary>
		/// <returns>The released view model, or <see langword="null"/> if none was created.</returns>
		public object? ReleaseViewModel()
		{
			var viewModel = _viewModel;
			_viewModel = null;
			(viewModel as IDisposable)?.Dispose();
			return viewModel;
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				ReleaseViewModel();
				if (Children is not null)
					foreach (var child in Children)
						child.Dispose();
			}
		}
	}

	/// <summary>
	/// A settings tree node that groups child nodes together.
	/// </summary>
	public class SettingsParentNode : SettingsTreeNode
	{
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
		/// Initializes a new instance of the <see cref="SettingsParentNode"/> class.
		/// </summary>
		/// <param name="name">The display name of the node.</param>
		/// <param name="icon">The icon shown next to the node.</param>
		/// <param name="children">The child nodes.</param>
		/// <param name="viewModelFactory">The factory that creates the view model shown when this node is selected.</param>
		public SettingsParentNode(string name, MaterialIconKind icon,
			List<SettingsTreeNode> children, Func<object?> viewModelFactory)
			: base(viewModelFactory)
		{
			DisplayName = name;
			Icon = icon;
			Children = children;
		}
	}

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
	/// A leaf settings tree node that shows a single settings category.
	/// </summary>
	public class SettingsLeafNode : SettingsTreeNode
	{
		/// <summary>
		/// Gets the display name of the node.
		/// </summary>
		public override string DisplayName { get; }

		/// <summary>
		/// Gets the icon shown next to the node.
		/// </summary>
		public override MaterialIconKind Icon { get; }

		/// <summary>
		/// Gets <see langword="null"/> because leaf nodes do not have children.
		/// </summary>
		public override IEnumerable<SettingsTreeNode>? Children => null;

		/// <summary>
		/// Initializes a new instance of the <see cref="SettingsLeafNode"/> class.
		/// </summary>
		/// <param name="name">The display name of the node.</param>
		/// <param name="icon">The icon shown next to the node.</param>
		/// <param name="viewModelFactory">The factory that creates the view model shown when this node is selected.</param>
		public SettingsLeafNode(string name, MaterialIconKind icon, Func<object?> viewModelFactory)
			: base(viewModelFactory)
		{
			DisplayName = name;
			Icon = icon;
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
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_users"),
				MaterialIconKind.AccountCircle,
				() => new ChatUserSettingsViewModel(Settings.Users.Users)));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("agents"),
				MaterialIconKind.Robot,
				() => new ChatAgentsSettingsViewModel(Settings.Agents, _agentManager)));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("settings_execution_stages"),
				MaterialIconKind.RobotConfused,
				() => new ChatExecutionStagesSettingsViewModel(Settings.Agents, _agentManager)));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_models"),
				MaterialIconKind.Brain,
				() => new ChatModelSettingsViewModel(Settings.Models)));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_environment"),
				MaterialIconKind.FolderSettings,
				() => new ChatEnvironmentSettingsViewModel(Settings.Environment,
					Chat.Services.GetServices<IScriptEngineEnvConfigurationProvider>(), Chat.Services.GetService<IExplorerOpener>())));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_databases"),
				MaterialIconKind.DatabaseSearch,
				() => new ChatDatabaseSettingsViewModel(Settings.Databases,
					Chat.Services.GetRequiredService<IApiKeyManagerService>(),
					Chat.Services.GetRequiredService<IDatabaseConnectionCache>())));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_mcp"),
				MaterialIconKind.Connection,
				() => new ChatMCPSettingsViewModel(Settings.Mcp, Chat.Services.GetRequiredService<IMCPManagementService>())));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_tools"),
				MaterialIconKind.Wrench,
				() => new ChatToolsSettingsViewModel(Settings.Tools)));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_skills"),
				MaterialIconKind.Cards,
				() => new ChatSkillsSettingsViewModel(Settings.Skills,
					Chat.Services.GetRequiredService<ISkillsetBuildingService>())));

			SettingsTree.Add(
				new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_memory"),
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
					new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_execution"),
						MaterialIconKind.Play,
						() => new AgentExecutionConditionsSettingsViewModel(descriptor.ExecutionConditions, Settings)),

					new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_llm_properties"),
						MaterialIconKind.Tune,
						() => new AgentGenerationSettingsViewModel(descriptor.Generation, Settings)),

					new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_read"),
						MaterialIconKind.Eye,
						() => new AgentReadSettingsViewModel(
							descriptor.Read, Settings.Agents.ChatAgents, descriptor.Id, Settings)),

					new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_prompts"),
						MaterialIconKind.Text,
						() => new AgentPromptSettingsViewModel(
							descriptor.Prompts,
							Settings,
							promptRegistry,
							Chat.Services.GetRequiredService<IChatPromptBuilder>(),
							descriptor)),

					new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_tools"),
						MaterialIconKind.Wrench,
						() => new AgentToolSettingsViewModel(
							descriptor.Tools,
							Chat.Services.GetRequiredService<IToolsetBuildingService>(),
							Settings)),

					new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_skills"),
						MaterialIconKind.Cards,
						() => new AgentSkillSettingsViewModel(
							descriptor.Skills,
							Chat.Services.GetRequiredService<ISkillsetBuildingService>(),
							Settings)),

					new SettingsLeafNode(LocalizationManager.LocalizeStatic("chat_settings_memory"),
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
