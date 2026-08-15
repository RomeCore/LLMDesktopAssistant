using LLMDesktopAssistant.Agents.Tasks;
using LLMDesktopAssistant.Agents.Tasks.MVVM;
using LLMDesktopAssistant.LLM.MVVM;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.MCP;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Settings.Application;
using LLMDesktopAssistant.Utils;
using Material.Icons;

namespace LLMDesktopAssistant.MVVM
{
	public class MainViewModelSidebarItemViewModel : NotifyPropertyChanged
	{
		public required MaterialIconKind Icon { get; init; }

		public required string Title { get; init; }

		public required object? Content { get; init; }

		private bool _isSelected;
		public bool IsSelected
		{
			get => _isSelected;
			set => SetProperty(ref _isSelected, value);
		}
	}

	public class MainViewModel : ViewModelBase
	{
		public RangeObservableCollection<MainViewModelSidebarItemViewModel> TopSidebarItems { get; }

		public RangeObservableCollection<MainViewModelSidebarItemViewModel> BottomSidebarItems { get; }

		private MainViewModelSidebarItemViewModel? _selectedTopSidebarItem;
		/// <summary>
		/// Gets or sets the currently selected top sidebar item. Selecting an item clears the bottom selection.
		/// </summary>
		public MainViewModelSidebarItemViewModel? SelectedTopSidebarItem
		{
			get => _selectedTopSidebarItem;
			set
			{
				if (SetProperty(ref _selectedTopSidebarItem, value))
				{
					if (value is not null)
					{
						SelectedBottomSidebarItem = null;
						SelectSidebarItem(value);
					}
				}
			}
		}

		private MainViewModelSidebarItemViewModel? _selectedBottomSidebarItem;
		/// <summary>
		/// Gets or sets the currently selected bottom sidebar item. Selecting an item clears the top selection.
		/// </summary>
		public MainViewModelSidebarItemViewModel? SelectedBottomSidebarItem
		{
			get => _selectedBottomSidebarItem;
			set
			{
				if (SetProperty(ref _selectedBottomSidebarItem, value))
				{
					if (value is not null)
					{
						SelectedTopSidebarItem = null;
						SelectSidebarItem(value);
					}
				}
			}
		}

		private MainViewModelSidebarItemViewModel? _selectedSidebarItem;
		/// <summary>
		/// Gets the currently selected sidebar item.
		/// </summary>
		public MainViewModelSidebarItemViewModel? SelectedSidebarItem => _selectedSidebarItem;

		private void SelectSidebarItem(MainViewModelSidebarItemViewModel item)
		{
			var prev = _selectedSidebarItem;
			if (SetProperty(ref _selectedSidebarItem, item))
			{
				prev?.IsSelected = false;
				item.IsSelected = true;
			}
		}

		public ChatManagerViewModel ChatManager { get; }
		public MCPManagerViewModel MCPManager { get; }
		public PromptManagerViewModel PromptManager { get; }
		public AgentTaskDispatcherViewModel AgentTaskDispatcher { get; }
		public ApplicationSettingsViewModel ApplicationSettings { get; }

		public MainViewModel()
		{
			TopSidebarItems = [];
			BottomSidebarItems = [];

			ChatManager = new ChatManagerViewModel(ChatServices.ManagementService);
			TopSidebarItems.Add(new MainViewModelSidebarItemViewModel
			{
				Icon = MaterialIconKind.Message,
				Title = "chat",
				Content = ChatManager
			});

			MCPManager = new MCPManagerViewModel();
			TopSidebarItems.Add(new MainViewModelSidebarItemViewModel
			{
				Icon = MaterialIconKind.Connection,
				Title = "mcp_manager_hint",
				Content = MCPManager
			});

			PromptManager = new PromptManagerViewModel();
			TopSidebarItems.Add(new MainViewModelSidebarItemViewModel
			{
				Icon = MaterialIconKind.Text,
				Title = "prompt_manager_hint",
				Content = PromptManager
			});

			AgentTaskDispatcher = new AgentTaskDispatcherViewModel(
				ServiceRegistry.Provider.GetRequiredService<IAgentTaskDispatcher>());
			TopSidebarItems.Add(new MainViewModelSidebarItemViewModel
			{
				Icon = MaterialIconKind.TimerSandComplete,
				Title = "agent_tasks",
				Content = AgentTaskDispatcher
			});

			ApplicationSettings = new ApplicationSettingsViewModel();
			BottomSidebarItems.Add(new MainViewModelSidebarItemViewModel
			{
				Icon = MaterialIconKind.Cog,
				Title = "settings_application",
				Content = ApplicationSettings
			});

			SelectedTopSidebarItem = TopSidebarItems[0];
		}
	}
}
