using LLMDesktopAssistant.LLM;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.ToolModules;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace LLMDesktopAssistant.Browsing
{
	/// <summary>
	/// View model for a browser tab.
	/// </summary>
	public class BrowserTabViewModel : ViewModelBase
	{
		private string _title = string.Empty;
		/// <summary>
		/// Gets or sets the title of the tab.
		/// </summary>
		public string Title
		{
			get => _title;
			set => SetProperty(ref _title, value);
		}

		private WebView2 _webView = new();
		/// <summary>
		/// The WebView2 control used to display the web content.
		/// </summary>
		public WebView2 WebView
		{
			get => _webView;
			set => SetProperty(ref _webView, value);
		}

		private ChatViewModel _chat = new();
		/// <summary>
		/// The LLM chat that applicated to this browser tab.
		/// </summary>
		public ChatViewModel Chat
		{
			get => _chat;
			set => SetProperty(ref _chat, value);
		}

		/// <summary>
		/// Gets the CoreWebView2 object associated with the WebView2 control.
		/// </summary>
		public CoreWebView2 CoreWebView2 => WebView.CoreWebView2;

		public BrowserTabViewModel()
		{
			_chat.AdditionalTools.Add(new BrowserToolModule(this));
		}
	}
}