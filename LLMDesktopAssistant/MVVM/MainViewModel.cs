using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Tabs;

namespace LLMDesktopAssistant.MVVM
{
	[ViewModelFor(typeof(MainView))]
	public class MainViewModel : ViewModelBase
	{
		public ObservableCollection<TabItem> Tabs { get; }

		private TabItem? _selectedTab;
		public TabItem? SelectedTab
		{
			get => _selectedTab;
			set => SetProperty(ref _selectedTab, value);
		}

		public MainViewModel()
		{
			var tabs = TabToolManager.Instantiate();
			Tabs = new(tabs.Select(t => new TabItem
			{
				Header = LocalizationManager.LocalizeStatic(t.Key),
				Content = t.Value
			}));
		}
	}
}