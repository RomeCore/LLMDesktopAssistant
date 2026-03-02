using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using LLMDesktopAssistant.Modules;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.ToolModules
{
	[Module]
	public class WebRequestToolModule : ToolModule
	{
		private readonly List<FunctionTool> _tools;
		private readonly HttpClient _httpClient;

		public WebRequestToolModule()
		{
			_tools = [];
			_httpClient = new HttpClient();

			// Добавляем базовые заголовки для имитации браузера
			_httpClient.DefaultRequestHeaders.Add("User-Agent",
				"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
			_httpClient.DefaultRequestHeaders.Add("Accept",
				"text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
			_httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
			_httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
			_httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
			_httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

			_tools.Add(FunctionTool.From(GetRequest, "web-get", "Perform a GET request to a specified URL."));
			_tools.Add(FunctionTool.From(PostRequest, "web-post", "Perform a POST request to a specified URL with JSON data."));
			_tools.Add(FunctionTool.From(DownloadFile, "web-download", "Download a file from a specified URL."));
			_tools.Add(FunctionTool.From(CheckWebsiteStatus, "web-status", "Check if a website is accessible and return status code."));
			_tools.Add(FunctionTool.From(ParseHtml, "web-parse", "Fetch HTML content and parse specific elements by tag or class."));
		}

		private async Task<ToolResult> GetRequest(
			[Description("URL to send GET request to")] string url,
			[Description("Optional: Additional headers as JSON string (e.g., {\"Authorization\": \"Bearer token\"})")] string headersJson = "")
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
				var responseContent = await response.Content.ReadAsStringAsync();

				var result = $"""
					Status code: {(int)response.StatusCode}
					Status description: {response.StatusCode.ToString()}
					Headers: {JsonSerializer.Serialize(response.Headers)}
					Content length: {responseContent.Length}
					Content:
					{responseContent}
					""";

				return new ToolResult(result);
			}
			catch (Exception ex)
			{
				return new ToolResult($"Error performing GET request: {ex.Message}");
			}
		}

		private async Task<ToolResult> PostRequest(
			[Description("URL to send POST request to")] string url,
			[Description("JSON data to send in the request body")] string jsonData,
			[Description("Content type (default: application/json)")] string contentType = "application/json")
		{
			try
			{
				var content = new StringContent(jsonData, Encoding.UTF8, contentType);
				var response = await _httpClient.PostAsync(url, content);
				var responseContent = await response.Content.ReadAsStringAsync();

				var result = $"""
					Status code: {(int)response.StatusCode}
					Status description: {response.StatusCode.ToString()}
					Headers: {JsonSerializer.Serialize(response.Headers)}
					Content length: {responseContent.Length}
					Content:
					{responseContent}
					""";

				return new ToolResult(result);
			}
			catch (Exception ex)
			{
				return new ToolResult($"Error performing POST request: {ex.Message}");
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
				return new ToolResult($"Error downloading file: {ex.Message}");
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
				return new ToolResult($"Error checking website status: {ex.Message}");
			}
		}

		private async Task<ToolResult> ParseHtml(
			[Description("URL to fetch HTML from")] string url,
			[Description("Tag name to parse (e.g., 'div', 'a', 'p')")] string tagName = "",
			[Description("Class name to filter by")] string className = "",
			[Description("Attribute to extract (e.g., 'href', 'src', 'title')")] string attribute = "")
		{
			try
			{
				var html = await _httpClient.GetStringAsync(url);
				var results = new List<Dictionary<string, object>>();

				// Simple HTML parsing using regex (for more complex parsing, consider HtmlAgilityPack)
				var matches = System.Text.RegularExpressions.Regex.Matches(
					html,
					$@"<{tagName}[^>]*class=[""']([^""']*{className}*[^""']*)[""'][^>]*>(.*?)</{tagName}>",
					System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline
				);

				foreach (System.Text.RegularExpressions.Match match in matches)
				{
					var result = new Dictionary<string, object>
					{
						["FullMatch"] = match.Value,
						["Content"] = match.Groups[2].Value.Trim()
					};

					// Extract attribute if specified
					if (!string.IsNullOrEmpty(attribute))
					{
						var attrMatch = System.Text.RegularExpressions.Regex.Match(
							match.Value,
							$@"{attribute}=[""']([^""']*)[""']",
							System.Text.RegularExpressions.RegexOptions.IgnoreCase
						);
						if (attrMatch.Success)
						{
							result[$"Attribute_{attribute}"] = attrMatch.Groups[1].Value;
						}
					}

					results.Add(result);
				}

				return new ToolResult(JsonSerializer.Serialize(new
				{
					Url = url,
					TotalMatches = results.Count,
					Results = results,
					Preview = results.Count > 0 ? results[0] : null
				}, new JsonSerializerOptions { WriteIndented = true }));
			}
			catch (Exception ex)
			{
				return new ToolResult($"Error parsing HTML: {ex.Message}");
			}
		}

		public override IEnumerable<ITool> GetTools()
		{
			return _tools;
		}

		// Optional: Dispose HttpClient when module is disposed
		public void Dispose()
		{
			_httpClient?.Dispose();
		}
	}
}