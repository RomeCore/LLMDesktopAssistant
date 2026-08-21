using System.ComponentModel;
using System.Text.Json.Nodes;
using AngleSharp.Html.Parser;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Localization;
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
		private readonly AsyncCache<(string Url, WebFetchLevel Level), FetchResult> _fetchContentCache;

		public WebFetchToolModule(IWebFetcher webFetcher)
		{
			_webFetcher = webFetcher;
			_fetchContentCache = new AsyncCache<(string Url, WebFetchLevel Level), FetchResult>(
				async ((string Url, WebFetchLevel Level) args, CancellationToken cancellationToken) =>
				{
					return await _webFetcher.FetchAsync(args.Url, args.Level, cancellationToken);
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
			if (schema["properties"]?.AsObject()["fetchLevel"] is not JsonObject fetchLevelProperty)
				return;
			if (fetchLevelProperty["enum"] is not JsonArray enumValues)
				return;

			var maxLevel = _webFetcher.MaxLevel;
			for (int i = enumValues.Count - 1; i >= 0; i--)
			{
				if (enumValues[i]?.GetValue<string>() is string value &&
					Enum.TryParse<WebFetchLevel>(value, ignoreCase: true, out var level) &&
					level > maxLevel)
				{
					enumValues.RemoveAt(i);
				}
			}
		}

		private static WebFetchLevel ParseLevel(string fetchLevel)
			=> Enum.TryParse<WebFetchLevel>(fetchLevel, ignoreCase: true, out var level) ? level : WebFetchLevel.HttpClient;

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
			[Description("The fetch level: plain HTTP request, headless browser (renders JavaScript), or stealth browser (evades advanced anti-bot checks). Escalates automatically when the page denies access.")]
			[Enum(["HttpClient", "Browser", "StealthBrowser"])]
			string fetchLevel = "HttpClient",
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
					var level = ParseLevel(fetchLevel);
					var html = await _fetchContentCache.GetAsync((url, level), cancellationToken);
					var content = contentType switch
					{
						"markdown" => HtmlToMarkdownConverter.Convert(html.Html),
						"sanitized_html" => HtmlSanitizer.Sanitize(html.Html),
						_ => html.Html
					};

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
			[Description("The fetch level: plain HTTP request, headless browser (renders JavaScript), or stealth browser (evades advanced anti-bot checks). Escalates automatically when the page denies access.")]
			[Enum(["HttpClient", "Browser", "StealthBrowser"])]
			string fetchLevel = "HttpClient",
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
					var level = ParseLevel(fetchLevel);
					var html = await _fetchContentCache.GetAsync((url, level), cancellationToken);
					var parser = new HtmlParser();
					var document = await parser.ParseDocumentAsync(html.Html);
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
