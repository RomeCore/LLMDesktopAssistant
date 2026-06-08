using Avalonia.Controls;
using LLMDesktopAssistant.LLM.MVVM;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Utils;
using Material.Icons;
using Material.Icons.Avalonia;

namespace LLMDesktopAssistant.MVVM
{
	public class MainViewModelSidebarItemViewModel : NotifyPropertyChanged
	{
		public required MaterialIconKind Icon { get; init; }

		public required string Title { get; init; }

		public required Dock Placement { get; init; }

		public required object? Content { get; init; }
	}

	public class MainViewModel : ViewModelBase
	{
		public RangeObservableCollection<MainViewModelSidebarItemViewModel> SidebarItems { get; }

		private MainViewModelSidebarItemViewModel? _selectedSidebarItem;
		public MainViewModelSidebarItemViewModel? SelectedSidebarItem
		{
			get => _selectedSidebarItem;
			set => SetProperty(ref _selectedSidebarItem, value);
		}

		public ChatManagerViewModel ChatManager { get; }

		public MainViewModel()
		{
			SidebarItems = [];

			ChatManager = new ChatManagerViewModel(ChatServices.ManagementService);
			SidebarItems.Add(new MainViewModelSidebarItemViewModel
			{
				Icon = MaterialIconKind.Chat,
				Title = "chat",
				Placement = Dock.Top,
				Content = ChatManager
			});

			SelectedSidebarItem = SidebarItems[0];
		}
	}
}
