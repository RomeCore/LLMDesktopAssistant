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
using LLMDesktopAssistant.Utils;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.ToolModules
{
	public class BrowserToolModule : ToolModule
	{
		private readonly BrowserTabViewModel _browserTab;
		private readonly List<FunctionTool> _tools;

		public BrowserToolModule(BrowserTabViewModel browserTab)
		{
			_browserTab = browserTab;

			_tools = [];
			_tools.Add(FunctionTool.From(NavigateToUrl, "browser-navigate_to_uri", "Navigates to a specified URL"));
			_tools.Add(FunctionTool.From(GetHTML, "browser-get_html", "Retrieves the HTML content of the current page"));
			_tools.Add(FunctionTool.From(ExecuteJavaScript, "browser-execute_javascript", "Executes JavaScript in the current page. Can return 'null' when script does not ends with line that returns value"));
		}

		public Task<ToolResult> NavigateToUrl([Description("URL to navigate to")] string url)
		{
			var tcs = new TaskCompletionSource<ToolResult>();

			App.Current.Dispatcher.BeginInvoke(async () =>
			{
				await _browserTab.WebView.EnsureCoreWebView2Async();
				_browserTab.CoreWebView2.NavigationCompleted += NavigationCompleted;
				_browserTab.CoreWebView2.Navigate(url);

				void NavigationCompleted(object? s, CoreWebView2NavigationCompletedEventArgs e)
				{
					if (e.IsSuccess)
					{
						tcs.SetResult($"Navigation successful. Status code: {e.HttpStatusCode}.");
					}
					else
					{
						tcs.SetResult($"Navigation not successful. Status code: {e.HttpStatusCode}, {e.WebErrorStatus}");
					}

					App.Current.Dispatcher.Invoke(() =>
					{
						_browserTab.CoreWebView2.NavigationCompleted -= NavigationCompleted;
					});
				}
			});

			return tcs.Task;
		}

		public async Task<ToolResult> GetHTML(
			[Description("Whether to sanitize HTML to remove extra data")] bool sanitize = true)
		{
			const string script = "document.documentElement.outerHTML";

			string result = string.Empty;
			await await App.Current.Dispatcher.InvokeAsync(async () =>
			{
				await _browserTab.WebView.EnsureCoreWebView2Async();
				var coreWeb = _browserTab.CoreWebView2;
				result = await coreWeb.ExecuteScriptAsync(script).ConfigureAwait(true);
			});

			result = Regex.Unescape(result)[1..^1];
			if (sanitize)
				result = HtmlUtils.Sanitize(result);

			const int maxCharacters = 40000;
			if (result.Length > maxCharacters)
				result = result[0..maxCharacters] + $" ... and {result.Length - maxCharacters} characters more...";

			return new ToolResult(result);
		}

		public async Task<ToolResult> ExecuteJavaScript([Description("JavaScript code to execute")] string script)
		{
			string result = string.Empty;
			await await App.Current.Dispatcher.InvokeAsync(async () =>
			{
				await _browserTab.WebView.EnsureCoreWebView2Async();
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