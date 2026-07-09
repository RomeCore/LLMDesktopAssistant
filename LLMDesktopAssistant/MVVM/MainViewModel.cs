using Avalonia.Controls;
using LLMDesktopAssistant.LLM.MVVM;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.MCP;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Utils;
using Material.Icons;
using Material.Icons.Avalonia;

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

		private MainViewModelSidebarItemViewModel? _selectedSidebarItem;
		public MainViewModelSidebarItemViewModel? SelectedSidebarItem
		{
			get => _selectedSidebarItem;
			set
			{
				var prev = _selectedSidebarItem;
				if (SetProperty(ref _selectedSidebarItem, value))
				{
					prev?.IsSelected = false;
					_selectedSidebarItem?.IsSelected = true;
				}
			}
		}

		public ChatManagerViewModel ChatManager { get; }
		public MCPManagerViewModel MCPManager { get; }
		public PromptManagerViewModel PromptManager { get; }

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

			SelectedSidebarItem = TopSidebarItems[0];
		}
	}
}
