using System.ComponentModel;
using AngleSharp;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils;
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

					var actualCount = Math.Min(count, content.Length - start);
					var slice = content.Substring(start, actualCount);

					result.ResultContent = $"""
						Url: {url}
						Total content length: {content.Length}
						Showing slice: {start}-{start + actualCount}
						Content slice:
						{slice}
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
					var config = Configuration.Default.WithDefaultLoader();
					var context = BrowsingContext.New(config);
					var document = await context.OpenAsync(url, cancellationToken);
					var elements = document.QuerySelectorAll(selector);
					var contents = elements.Select(m => m.TextContent);

					var html = string.Join("\n\n", contents);
					var actualCount = Math.Min(count, html.Length - start);
					var slice = html.Substring(start, actualCount);

					result.ResultContent = $"""
						```html
						{slice}
						```
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

		private static readonly ReverseMarkdown.Converter _mdConverter = new(CreateMdConfig());

		private static ReverseMarkdown.Config CreateMdConfig()
		{
			var result = new ReverseMarkdown.Config
			{
				GithubFlavored = true,
			};

			result.Tags.Unknown = ReverseMarkdown.Config.UnknownTagsOption.Drop;
			result.Formatting.RemoveComments = true;
			result.Images.Base64Handling = ReverseMarkdown.Config.Base64ImageHandling.Skip;

			return result;
		}

		private readonly AsyncCache<(string, string), string> _fetchContentCache = new(
			async ((string url, string contentType) args) =>
			{
				var config = Configuration.Default.WithDefaultLoader();
				var context = BrowsingContext.New(config);
				var document = await context.OpenAsync(args.url);

				var content = document.Body?.OuterHtml ?? string.Empty;
				switch (args.contentType)
				{
					case "sanitized_html":
						content = HtmlUtils.Sanitize(content);
						break;

					case "markdown":
						content = _mdConverter.Convert(content);
						break;
				}

				return content;
			}, slidingExpirationTime: TimeSpan.FromMinutes(5));

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