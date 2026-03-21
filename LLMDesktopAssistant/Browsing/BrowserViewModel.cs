using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using LLMDesktopAssistant.LLM.Data;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.Tabs;

namespace LLMDesktopAssistant.Browsing
{
	[ViewModelFor(typeof(BrowserView))]
	[TabTool("browser", Order = 10)]
	public class BrowserViewModel : ViewModelBase
	{
		private class TabCollection : IEnumerable<TabItem>, INotifyCollectionChanged
		{
			private readonly BrowserViewModel _browser;

			public TabCollection(BrowserViewModel browser)
			{
				_browser = browser;
				_browser.TabItems.CollectionChanged += (s, e) =>
					CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			}

			public event NotifyCollectionChangedEventHandler? CollectionChanged;

			public IEnumerator<TabItem> GetEnumerator()
			{
				return _browser.TabItems
					.Select(t => new TabItem { Header = t.Title, Content = t.WebView })
					.GetEnumerator();
			}
			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		/// <summary>
		/// Collection of browser tabs.
		/// </summary>
		public ObservableCollection<BrowserTabViewModel> TabItems { get; } = [];

		/// <summary>
		/// Gets an observable collection of TabItems for use in data binding.
		/// </summary>
		public IEnumerable<TabItem> ObservableTabs { get; }

		private BrowserTabViewModel? _selectedTab;
		/// <summary>
		/// Gets or sets the currently selected tab.
		/// </summary>
		public BrowserTabViewModel? SelectedTab
		{
			get => _selectedTab;
			set => SetProperty(ref _selectedTab, value);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BrowserViewModel"/> class.
		/// </summary>
		public BrowserViewModel()
		{
			ObservableTabs = new TabCollection(this);

			var db = new ConversationDatabase("conversations/browser.db");

			if (db.Conversations.FindById(1) == null)
				db.Conversations.Insert(new LLM.Data.Models.ConversationModel
				{
					Id = 1,
					SystemInstructions = "You are a helpful assistant."
				});
			var conversationManager = new ConversationManager(db, 1);

			var tab = new BrowserTabViewModel(conversationManager) { Title = "Home" };
			tab.WebView.Source = new Uri("https://www.google.com");
			TabItems.Add(tab);
		}
	}
}