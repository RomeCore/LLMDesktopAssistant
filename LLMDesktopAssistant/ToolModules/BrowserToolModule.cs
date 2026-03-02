using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using Ganss.Xss;
using LLMDesktopAssistant.Browsing;
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.Tabs;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.ToolModules
{
	[Module]
	public class BrowserToolModule : ToolModule
	{
		private readonly BrowserViewModel _browser;
		private readonly BrowserTabViewModel _browserTab;
		private readonly HtmlSanitizer _sanitizer;
		private readonly List<FunctionTool> _tools;

		public BrowserToolModule()
		{
			_browser = TabToolManager.Get<BrowserViewModel>("browser");
			_browserTab = new BrowserTabViewModel { Title = "Tooled Tab" };
			_sanitizer = new HtmlSanitizer();
			_browser.TabItems.Add(_browserTab);

			_tools = [];
			_tools.Add(FunctionTool.From(NavigateToUrl, "browser-navigate_to_uri", "Navigates to a specified URL"));
			_tools.Add(FunctionTool.From(GetHTML, "browser-get_html", "Retrieves the HTML content of the current page"));
			_tools.Add(FunctionTool.From(ExecuteJavaScript, "browser-execute_javascript", "Executes JavaScript in the current page"));
		}

		public async Task<ToolResult> NavigateToUrl([Description("URL to navigate to")] string url)
		{
			await App.Current.Dispatcher.InvokeAsync(() =>
			{
				_browserTab.WebView.Source = new Uri(url);
			});

			return new ToolResult("Navigation successful.");
		}

		public async Task<ToolResult> GetHTML()
		{
			const string script = "document.documentElement.outerHTML";

			string result = string.Empty;
			await await App.Current.Dispatcher.InvokeAsync(async () =>
			{
				var coreWeb = _browserTab.CoreWebView2;
				result = await coreWeb.ExecuteScriptAsync(script).ConfigureAwait(true);
			});

			result = Regex.Unescape(result)[1..^1];
			result = _sanitizer.Sanitize(result);
			return new ToolResult(result);
		}

		public async Task<ToolResult> ExecuteJavaScript([Description("JavaScript code to execute")] string script)
		{
			string result = string.Empty;
			await await App.Current.Dispatcher.InvokeAsync(async () =>
			{
				var coreWeb = _browserTab.CoreWebView2;
				result = await coreWeb.ExecuteScriptAsync(script).ConfigureAwait(true);
			});

			return new ToolResult(result ?? "Failed to execute JavaScript.");
		}

		public override IEnumerable<ITool> GetTools()
		{
			return _tools;
		}
	}
}