using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LLMDesktopAssistant.Core.Localization;
using LLMDesktopAssistant.Core.Tabs;
using MaterialDesignThemes.Wpf;

namespace LLMDesktopAssistant.Core.MVVM
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
			var tabs = TabToolManager.TabTools;
			Tabs = new(tabs
				.OrderBy(t => t.Value.Order)
				.Select(t => new TabItem
			{
				Header = BuildHeader(t.Value),
				Content = t.Value.View
			}));
			SelectedTab = Tabs.FirstOrDefault();
		}

		private object BuildHeader(TabToolInfo tabTool)
		{
			var stackPanel = new StackPanel
			{
				Orientation = Orientation.Vertical
			};

			stackPanel.Children.Add(new PackIcon
			{
				Width = 24,
				Height = 24,
				Kind = tabTool.Icon,
				HorizontalAlignment = HorizontalAlignment.Center
			});
			stackPanel.Children.Add(new TextBlock { Text = LocalizationManager.LocalizeStatic(tabTool.Id) });

			return stackPanel;
		}
	}
}