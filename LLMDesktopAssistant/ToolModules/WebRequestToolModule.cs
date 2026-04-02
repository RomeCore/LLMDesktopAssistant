using AngleSharp;
using Ganss.Xss;
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.Utils;
using RCLargeLanguageModels.Json.Schema;
using RCLargeLanguageModels.Security;
using RCLargeLanguageModels.Tools;
using RCLargeLanguageModels.Utilities;
using RCParsing;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LLMDesktopAssistant.ToolModules
{
	[Module]
	public class WebRequestToolModule : ToolModule
	{
		private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
		{
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
			WriteIndented = true
		};

		private readonly HttpClient _httpClient;

		public WebRequestToolModule()
		{
			_httpClient = new HttpClient();

			_httpClient.DefaultRequestHeaders.Add("User-Agent",
				"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
			_httpClient.DefaultRequestHeaders.Add("Accept",
				"text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
			_httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
			_httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
			_httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(WebRequest, "web-request", "Perform a request to a specified URL and method."),
				Category = "web"
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(DownloadFile, "web-download", "Download a file from a specified URL."),
				Category = "web"
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(CheckWebsiteStatus, "web-status", "Check if a website is accessible and return status code."),
				Category = "web"
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(GetHtml, "web-get_html", "Fetch HTML content from a specified URL."),
				Category = "web"
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(ParseHtml, "web-parse", "Fetch HTML content and parse specific elements by tag or class."),
				Category = "web"
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(Search_SearXNG, "web-search", "Search through the web using query."),
				Category = "web"
			});
		}

		private async Task<ToolResult> WebRequest(
			[Description("Method of the request"), Enum(["GET", "POST", "PUT", "DELETE"])] string method,
			[Description("URL to send request to")] string url,
			[Description("Optional: Additional headers as JSON string (e.g., {\"Authorization\": \"Bearer token\"})")] string headersJson = "",
			[Description("Optional: Request content")] string content = "",
			[Description("Content type (default: application/json)")] string contentType = "application/json")
		{
			try
			{
				var httpMethod = method switch
				{
					"GET" => HttpMethod.Get,
					"POST" => HttpMethod.Post,
					"PUT" => HttpMethod.Put,
					"DELETE" => HttpMethod.Delete,
					_ => throw new ArgumentException("Invalid HTTP method", nameof(method))
				};

				using var request = new HttpRequestMessage(httpMethod, url);

				if (!string.IsNullOrEmpty(content))
					request.Content = new StringContent(content, Encoding.UTF8, contentType);

				if (!string.IsNullOrEmpty(headersJson))
				{
					var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
					foreach (var header in headers!)
					{
						request.Headers.TryAddWithoutValidation(header.Key, header.Value);
					}
				}

				var response = await _httpClient.SendAsync(request);
				var responseContent = await response.Content.ReadAsStringAsync();

				const int maxCharacters = 35000;
				if (responseContent.Length > maxCharacters)
					responseContent = responseContent[0..maxCharacters] + $" ... and {responseContent.Length - maxCharacters} characters more...";

				var result = $"""
					Status code: {(int)response.StatusCode}
					Status description: {response.StatusCode.ToString()}
					Headers: {JsonSerializer.Serialize(response.Headers)}
					Content length: {responseContent.Length}
					Content:
					```
					{responseContent}
					```
					""";

				return new ToolResult(result);
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error performing GET request: {ex.Message}");
			}
		}

		private async Task<ToolResult> DownloadFile(
			[Description("URL of the file to download")] string url,
			[Description("Local path to save the file")] string savePath,
			[Description("Optional: Additional headers as JSON string")] string headersJson = "")
		{
			try
			{
				using var request = new HttpRequestMessage(HttpMethod.Get, url);

				if (!string.IsNullOrEmpty(headersJson))
				{
					var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
					foreach (var header in headers!)
					{
						request.Headers.TryAddWithoutValidation(header.Key, header.Value);
					}
				}

				var response = await _httpClient.SendAsync(request);
				response.EnsureSuccessStatusCode();

				var fileBytes = await response.Content.ReadAsByteArrayAsync();

				// Ensure directory exists
				var directory = System.IO.Path.GetDirectoryName(savePath);
				if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
				{
					System.IO.Directory.CreateDirectory(directory);
				}

				await System.IO.File.WriteAllBytesAsync(savePath, fileBytes);

				var result = $"""
					Status code: {(int)response.StatusCode}
					Status description: {response.StatusCode.ToString()}
					FilePath: {savePath}
					FileSize: {fileBytes.Length}
					Content type: {response.Content.Headers.ContentType?.ToString()}
					Content length: {fileBytes.Length}
					""";

				return new ToolResult(result);
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error downloading file: {ex.Message}");
			}
		}

		private async Task<ToolResult> CheckWebsiteStatus(
			[Description("URL to check")] string url,
			[Description("Timeout in seconds (default: 30)")] int timeoutSeconds = 30)
		{
			try
			{
				using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
				var startTime = DateTime.UtcNow;

				var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
				var endTime = DateTime.UtcNow;

				var result = $"""
					Url: {url}
					Status code: {(int)response.StatusCode}
					Status description: {response.StatusCode.ToString()}
					Is accessible: {response.IsSuccessStatusCode}
					Response time ms: {(endTime - startTime).TotalMilliseconds}
					Content type: {response.Content.Headers.ContentType?.ToString()}
					Content length: {response.Content.Headers.ContentLength}
					Server: {response.Headers.Server?.ToString() ?? "Unknown"}
					""";

				return new ToolResult(result);
			}
			catch (TaskCanceledException)
			{
				return new ToolResult($"Website check timeout after {timeoutSeconds} seconds for URL: {url}");
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error checking website status: {ex.Message}");
			}
		}

		private async Task<ToolResult> GetHtml(
			[Description("URL to fetch HTML from")] string url,
			[Description("Whether to sanitize HTML to remove extra data")] bool sanitize = true)
		{
			try
			{
				var config = Configuration.Default.WithDefaultLoader();
				var context = BrowsingContext.New(config);
				var document = await context.OpenAsync(url);

				var html = document.Body?.OuterHtml ?? string.Empty;

				if (sanitize)
					html = HtmlUtils.Sanitize(html);

				const int maxCharacters = 35000;
				if (html.Length > maxCharacters)
					html = html[0..maxCharacters] + $" ... and {html.Length - maxCharacters} characters more...";

				var result = $"""
					```html
					{html}
					```
					""";
				return new ToolResult(result);
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error getting HTML: {ex.Message}");
			}
		}
		
		private async Task<ToolResult> ParseHtml(
			[Description("URL to fetch HTML from")] string url,
			[Description("The query selector to select values with")] string selector)
		{
			try
			{
				var config = Configuration.Default.WithDefaultLoader();
				var context = BrowsingContext.New(config);
				var document = await context.OpenAsync(url);
				var elements = document.QuerySelectorAll(selector);
				var contents = elements.Select(m => m.TextContent);

				var html = string.Join("\n\n", contents);
				const int maxCharacters = 35000;
				if (html.Length > maxCharacters)
					html = html[0..maxCharacters] + $" ... and {html.Length - maxCharacters} characters more...";

				var result = $"""
					```html
					{html}
					```
					""";
				return new ToolResult(result);
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error parsing HTML: {ex.Message}");
			}
		}
		
		private async Task<ToolResult> Search_LangSearch(
			[Description("The query to search by")] string query,
			[Description("The maximum number of results to return")] int maxResults = 10,
			[Description("Whether to show long text summaries for results")] bool provideSummary = false)
		{
			try
			{
				var request = new HttpRequestMessage(HttpMethod.Post, "https://api.langsearch.com/v1/web-search");

				var apiKey = new EnvironmentTokenAccessor("LANGSEARCH_API_KEY").GetToken();
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

				var body = new JsonObject
				{
					["query"] = query,
					["freshness"] = "noLimit",
					["summary"] = provideSummary,
					["count"] = maxResults,
				};
				request.Content = JsonContent.Create(body);
				var response = await _httpClient.SendAsync(request);
				response.EnsureSuccessStatusCode();
				var responseContent = await response.ParseContentAsync<JsonObject>();

				var pageData = responseContent?["data"]?["webPages"]?["value"]!;

				var sb = new StringBuilder();

				foreach (var item in (JsonArray)pageData)
				{
					var name = item?["name"]?.ToString() ?? "Unknown";
					var url = item?["url"]?.ToString() ?? "Unknown";
					var snippet = item?["snippet"]?.ToString();
					var summary = item?["summary"]?.ToString();

					sb.AppendLine($"**{name}**: [{url}]");
					if (summary == null)
						sb.AppendLine(snippet);
					else
						sb.AppendLine(summary);
					sb.AppendLine();
				}

				return new ToolResult(sb.ToString().Trim());
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error using search: {ex.Message}");
			}
		}

		private async Task<ToolResult> Search_Jina(
			[Description("The query to search by")] string query,
			[Description("The page number to return results for")] int page = 1)
		{
			try
			{
				var encodedQuery = Uri.EscapeDataString(query);
				var request = new HttpRequestMessage(
					HttpMethod.Get,
					$"https://s.jina.ai/?q={encodedQuery}&page={page}");
				request.Headers.Add("X-Respond-With", "no-content");

				var apiKey = new EnvironmentTokenAccessor("JINA_API_KEY").GetToken();
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

				var response = await _httpClient.SendAsync(request);
				response.EnsureSuccessStatusCode();
				var responseContent = await response.Content.ReadAsStringAsync();

				// Jina AI returns plain markdown text that suitable for LLM, so we can just return it directly.
				return new ToolResult(responseContent);
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error using search: {ex.Message}");
			}
		}

		private async Task<ToolResult> Search_SearXNG(
			[Description("The query to search by")] string query,
			[Description("The page number to return results for"), Range(1, 10)] int page = 1,
			[Enum(["general", "images", "videos", "news", "map", "music", "it", "science", "files", "social media"])] string category = "general",
			[Description("Language code (auto, en, ru, etc.)")] string language = "auto",
			[Enum(["day", "week", "month", "year"])] string timeRange = "",
			[Enum(["none", "moderate", "strict"])] string safeSearch = "none")
		{
			try
			{
				var searxngUrl = new EnvironmentTokenAccessor("SEARXNG_URL").GetToken() ?? "http://localhost:8080";

				int safeSearchIndex = safeSearch switch
				{
					"none" => 0,
					"moderate" => 1,
					"strict" => 2,
					_ => throw new ArgumentException("Invalid safe search option", nameof(safeSearch))
				};
				var parameters = new Dictionary<string, string>
				{
					["q"] = query,
					["pageno"] = page.ToString(),
					["format"] = "json",
					["categories"] = category,
					["language"] = language,
					["safesearch"] = safeSearchIndex.ToString()
				};

				if (!string.IsNullOrEmpty(timeRange))
				{
					parameters["time_range"] = timeRange;
				}

				var queryString = string.Join("&", parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
				var requestUrl = $"{searxngUrl}/search?{queryString}";

				using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

				request.Headers.Add("Accept", "application/json");

				var response = await _httpClient.SendAsync(request);
				response.EnsureSuccessStatusCode();
				var responseContent = await response.ParseContentAsync<JsonObject>();

				var results = responseContent?["results"]!;
				var sb = new StringBuilder();

				foreach (var item in ((JsonArray)results).Take(50))
				{
					var title = item?["title"]?.ToString() ?? "Unknown";
					var url = item?["url"]?.ToString() ?? "Unknown";
					var content = item?["content"]?.ToString();
					var imgSrc = item?["img_src"]?.ToString();

					sb.AppendLine($"[{title}]({url}):");
					if (!string.IsNullOrEmpty(imgSrc))
						sb.AppendLine($"![Image]({imgSrc})");
					sb.AppendLine(content);
					sb.AppendLine();
				}

				return new ToolResult(sb.ToString().Trim());
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error using SearXNG search: {ex.Message}");
			}
		}

		public void Dispose()
		{
			_httpClient?.Dispose();
		}
	}
}