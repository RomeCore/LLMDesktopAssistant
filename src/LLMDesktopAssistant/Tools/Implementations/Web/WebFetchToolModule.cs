using System.ComponentModel;
using System.Text.Json.Nodes;
using AngleSharp.Html.Parser;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Settings.Application;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Web;
using Material.Icons;
using RCLargeLanguageModels.Json.Schema;

namespace LLMDesktopAssistant.Tools.Implementations.Web
{
	[ToolModule(chatScoped: true)]
	public class WebFetchToolModule : ToolModule
	{
		private readonly IWebFetcher _webFetcher;
		private readonly AsyncCache<(string, string, bool, bool), string> _fetchContentCache;

		public WebFetchToolModule(IWebFetcher webFetcher)
		{
			_webFetcher = webFetcher;
			_fetchContentCache = new AsyncCache<(string, string, bool, bool), string>(
				async ((string url, string contentType, bool useBrowser, bool useStealthBrowser) args, CancellationToken cancellationToken) =>
				{
					var content = await _webFetcher.FetchContentAsync(args.url, new WebFetchOptions(args.useBrowser, args.useStealthBrowser), cancellationToken);
					switch (args.contentType)
					{
						case "sanitized_html":
							content = HtmlSanitizer.Sanitize(content);
							break;

						case "markdown":
							content = HtmlToMarkdownConverter.Convert(content);
							break;
					}
					return content;
				}, slidingExpirationTime: TimeSpan.FromMinutes(15));

			AddTool(new ToolInitializationInfo
			{
				Executor = Fetch,
				StreamingAnalyzer = FetchStreaming,
				Name = "web-fetch",
				Description = "Fetch webcite content from a specified URL.",
				TitleKey = Locale.GetKey("tool.name.web-fetch"),
				DescriptionKey = Locale.GetKey("tool.description.web-fetch"),
				CategoryKey = Locale.GetKey("tool.category.web"),
				DefaultExpectedBehaviour = ToolBehaviour.InternetAccess,
				ModifyArgumentSchema = RemoveUnsupportedArguments
			});

			AddTool(new ToolInitializationInfo
			{
				Executor = ParseHtml,
				StreamingAnalyzer = ParseHtmlStreaming,
				Name = "web-parse",
				Description = "Fetch HTML content and parse specific elements by tag or class.",
				TitleKey = Locale.GetKey("tool.name.web-parse"),
				DescriptionKey = Locale.GetKey("tool.description.web-parse"),
				CategoryKey = Locale.GetKey("tool.category.web"),
				DefaultExpectedBehaviour = ToolBehaviour.InternetAccess,
				ModifyArgumentSchema = RemoveUnsupportedArguments
			});
		}

		private void RemoveUnsupportedArguments(JsonObject schema)
		{
			if (!_webFetcher.SupportsBrowser)
				schema["properties"]?.AsObject().Remove("useBrowser");
			if (!_webFetcher.SupportsStealthBrowser)
				schema["properties"]?.AsObject().Remove("useStealthBrowser");
		}

		private WebFetchOptions ResolveOptions(bool useBrowser, bool useStealthBrowser)
		{
			var settings = ApplicationSettingsAccessor.ApplicationSettings.WebFetch;
			return new WebFetchOptions(useBrowser || settings.UseBrowser, useStealthBrowser || settings.UseStealthBrowser);
		}

		private StreamingToolArgumentsAnalysisResult FetchStreaming(
			string? url)
		{
			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.Web,
				StatusTitle = $"`{url}`"
			};
		}

		private async Task<ReactiveToolResult> Fetch(
			[Description("URL to fetch HTML from")]
			string url,
			[Description("The starting index of character to return")]
			int start = 0,
			[Description("The maximum count of characters to return")]
			int count = 10000,
			[Description("The content type to fetch")]
			[Enum(["html", "sanitized_html", "markdown"])]
			string contentType = "markdown",
			[Description("Load the page with a headless browser, rendering JavaScript. Slower, but bypasses basic anti-bot checks.")]
			bool useBrowser = false,
			[Description("Load the page with a stealth CloakBrowser, evading advanced anti-bot checks (Cloudflare, reCAPTCHA v3). Slowest option.")]
			bool useStealthBrowser = false,
			CancellationToken cancellationToken = default)
		{
			var result = new ReactiveToolResult
			{
				StatusIcon = MaterialIconKind.Web,
				StatusTitle = $"`{url}`"
			};

			_ = Task.Run(async () =>
			{
				try
				{
					var content = await _fetchContentCache.GetAsync((url, contentType, useBrowser, useStealthBrowser), cancellationToken);

					start = Math.Max(Math.Min(start, content.Length), 0);
					count = Math.Min(count, content.Length - start);
					int end = start + count;
					var slice = content.Substring(start, count);

					string afterTip = end < content.Length ? $"\n*Can read {content.Length - end} more characters. Call tool again with same arguments (but with new `start` and `count` values) to read more.*" : "";

					result.ResultContent = $"""
						**Url**: *{url}*
						**Showing slice**: *{start}-{start + count}* from *{content.Length}*
						[CONTENT START]
						{slice}
						[CONTENT END]{afterTip}
						""";

					result.CompleteWithSuccess();
				}
				catch (Exception ex)
				{
					result.ResultContent = $"Error fetching web content: {ex.Message}";
					result.CompleteWithError();
				}
			});

			return result;
		}

		private StreamingToolArgumentsAnalysisResult ParseHtmlStreaming(
			string? url, string? selector)
		{
			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.Web,
				StatusTitle = selector != null ? $"`{url}` → `{selector}`" : $"`{url}`"
			};
		}

		private async Task<ReactiveToolResult> ParseHtml(
			[Description("URL to fetch HTML from")]
			string url,
			[Description("The query selector to select values with")]
			string selector,
			[Description("Load the page with a headless browser, rendering JavaScript. Slower, but bypasses basic anti-bot checks.")]
			bool useBrowser = false,
			[Description("Load the page with a stealth CloakBrowser, evading advanced anti-bot checks (Cloudflare, reCAPTCHA v3). Slowest option.")]
			bool useStealthBrowser = false,
			[Description("The starting index of character to return")]
			int start = 0,
			[Description("The maximum count of characters to return")]
			int count = 10000,
			CancellationToken cancellationToken = default)
		{
			var result = new ReactiveToolResult
			{
				StatusIcon = MaterialIconKind.Web,
				StatusTitle = $"`{url}` → `{selector}`"
			};

			_ = Task.Run(async () =>
			{
				try
				{
					var html = await _webFetcher.FetchContentAsync(url, ResolveOptions(useBrowser, useStealthBrowser), cancellationToken);
					var parser = new HtmlParser();
					var document = await parser.ParseDocumentAsync(html);
					var elements = document.QuerySelectorAll(selector);
					var contents = elements.Select(m => m.TextContent);

					var content = string.Join("\n\n", contents);
					start = Math.Max(Math.Min(start, content.Length), 0);
					count = Math.Min(count, content.Length - start);
					int end = start + count;
					var slice = content.Substring(start, count);

					string afterTip = end < content.Length ? $"\n*Can read {content.Length - end} more characters. Call tool again with same arguments (but with new `start` and `count` values) to read more.*" : "";

					result.ResultContent = $"""
						**Url**: *{url}*
						**Selector**: *{selector}*
						**Showing slice**: *{start}-{start + count}* from *{content.Length}*
						[CONTENT START]
						{slice}
						[CONTENT END]{afterTip}
						""";
					result.CompleteWithSuccess();
				}
				catch (Exception ex)
				{
					result.ResultContent = $"Error parsing HTML: {ex.Message}";
					result.CompleteWithError();
				}
			});

			return result;
		}

		}
}
