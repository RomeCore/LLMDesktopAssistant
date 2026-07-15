using System.ComponentModel;
using AngleSharp;
using AngleSharp.Html.Parser;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Web;
using Material.Icons;
using RCLargeLanguageModels.Json.Schema;

namespace LLMDesktopAssistant.Tools.Implementations.Web
{
	[ToolModule(chatScoped: true)]
	public class WebFetchToolModule : ToolModule
	{
		private readonly HttpClient _httpClient, _httpInfiniteTimeoutClient;
		private readonly WorkingDirectoryAccessService _fileAccess;

		public WebFetchToolModule(WorkingDirectoryAccessService fileAccess)
		{
			_httpClient = CreateClient();
			_httpInfiniteTimeoutClient = CreateClient();
			_httpInfiniteTimeoutClient.Timeout = Timeout.InfiniteTimeSpan;

			_fileAccess = fileAccess;

			AddTool(Fetch, FetchStreaming, null,
				new ToolInitializationInfo
				{
					Name = "web-fetch",
					Description = "Fetch webcite content from a specified URL.",
					Category = "web",
					DefaultExpectedBehaviour = ToolBehaviour.InternetAccess
				});

			AddTool(ParseHtml, ParseHtmlStreaming, null,
				new ToolInitializationInfo
				{
					Name = "web-parse",
					Description = "Fetch HTML content and parse specific elements by tag or class.",
					Category = "web",
					DefaultExpectedBehaviour = ToolBehaviour.InternetAccess
				});
		}

		public StreamingToolArgumentsAnalysisResult FetchStreaming(
			string? url)
		{
			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.Web,
				StatusTitle = $"`{url}`"
			};
		}

		public async Task<ReactiveToolResult> Fetch(
			[Description("URL to fetch HTML from")]
			string url,
			[Description("The starting index of character to return")]
			int start = 0,
			[Description("The maximum count of characters to return")]
			int count = 10000,
			[Description("The content type to fetch")]
			[Enum(["html", "sanitized_html", "markdown"])]
			string contentType = "markdown",
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
					var content = await _fetchContentCache.GetAsync((url, contentType), cancellationToken);

					start = Math.Max(Math.Min(start, content.Length), 0);
					count = Math.Min(count, content.Length - start);
					int end = start + count;
					var slice = content.Substring(start, count);

					string afterTip = end < content.Length ? $"\n*Can read {end - start} more characters. Call tool again with same arguments (but with new `start` and `count` values) to read more.*" : "";

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

		public StreamingToolArgumentsAnalysisResult ParseHtmlStreaming(
			string? url, string? selector)
		{
			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.Web,
				StatusTitle = selector != null ? $"`{url}` → `{selector}`" : $"`{url}`"
			};
		}

		public async Task<ReactiveToolResult> ParseHtml(
			[Description("URL to fetch HTML from")]
			string url,
			[Description("The query selector to select values with")]
			string selector,
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
					var html = await HtmlContentFetcher.FetchContentAsync(url);
					var parser = new HtmlParser();
					var document = await parser.ParseDocumentAsync(html);
					var elements = document.QuerySelectorAll(selector);
					var contents = elements.Select(m => m.TextContent);

					var parsedHtml = string.Join("\n\n", contents);
					start = Math.Max(Math.Min(start, parsedHtml.Length), 0);
					count = Math.Min(count, parsedHtml.Length - start);
					int end = start + count;
					var slice = parsedHtml.Substring(start, count);

					string afterTip = end < parsedHtml.Length ? $"\n*Can read {end - start} more characters. Call tool again with same arguments (but with new `start` and `count` values) to read more.*" : "";

					result.ResultContent = $"""
						**Url**: *{url}*
						**Selector**: *{selector}*
						**Showing slice**: *{start}-{start + count}* from *{parsedHtml.Length}*
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

		protected override void Dispose(bool disposing)
		{
			_httpClient.Dispose();
			_httpInfiniteTimeoutClient.Dispose();
		}

		private readonly AsyncCache<(string, string), string> _fetchContentCache = new(
			async ((string url, string contentType) args, CancellationToken cancellationToken) =>
			{
				var content = await HtmlContentFetcher.FetchContentAsync(args.url, cancellationToken);
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

		private static HttpClient CreateClient()
		{
			var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("User-Agent",
				"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
			httpClient.DefaultRequestHeaders.Add("Accept",
				"text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
			httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
			httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
			httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
			return httpClient;
		}
	}
}