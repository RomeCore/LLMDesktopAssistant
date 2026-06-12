using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AngleSharp;
using Ganss.Xss;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Utils.Files;
using Material.Icons;
using RCLargeLanguageModels.Json.Schema;
using RCLargeLanguageModels.Security;
using RCLargeLanguageModels.Tools;
using RCLargeLanguageModels.Utilities;
using RCParsing;

namespace LLMDesktopAssistant.Tools.Implementations
{
	[ToolModule]
	public class WebRequestToolModule : ToolModule
	{
		private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
		{
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
			WriteIndented = true
		};

		private readonly HttpClient _httpClient, _httpInfiniteTimeoutClient;
		private readonly FileAccessService _fileAccess;

		public WebRequestToolModule(FileAccessService fileAccess)
		{
			_httpClient = CreateClient();
			_httpInfiniteTimeoutClient = CreateClient();
			_httpInfiniteTimeoutClient.Timeout = Timeout.InfiniteTimeSpan;

			_fileAccess = fileAccess;

			AddTool(WebRequest,
				new ToolInitializationInfo
				{
					Name = "web-request",
					Description = "Perform a request to a specified URL and method.",
					Category = "web",
					DefaultExpectedBehaviour = ToolBehaviour.InternetAccess
				});

			AddTool(DownloadFile, DownloadFileStream, null,
				new ToolInitializationInfo
				{
					Name = "web-download",
					Description = "Download a file from a specified URL into the working directory.",
					Category = "web",
					DefaultExpectedBehaviour = ToolBehaviour.FileDirectoryCreate | ToolBehaviour.FileEdit | ToolBehaviour.InternetAccess
				});

			AddTool(CheckWebsiteStatus,
				new ToolInitializationInfo
				{
					Name = "web-status",
					Description = "Check if a website is accessible and return status code.",
					Category = "web",
					DefaultExpectedBehaviour = ToolBehaviour.InternetAccess
				});

			AddTool(Fetch,
				new ToolInitializationInfo
				{
					Name = "web-fetch",
					Description = "Fetch webcite content from a specified URL.",
					Category = "web",
					DefaultExpectedBehaviour = ToolBehaviour.InternetAccess
				});

			AddTool(ParseHtml,
				new ToolInitializationInfo
				{
					Name = "web-parse",
					Description = "Fetch HTML content and parse specific elements by tag or class.",
					Category = "web",
					DefaultExpectedBehaviour = ToolBehaviour.InternetAccess
				});
		}

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

		private async Task<ToolResult> WebRequest(
			[Description("Method of the request"), Enum(["GET", "POST", "PUT", "DELETE"])] string method,
			[Description("URL to send request to")] string url,
			[Description("Optional: Additional headers as JSON")] JsonObject? headersJson = null,
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

				if (headersJson != null)
				{
					foreach (var header in headersJson)
					{
						request.Headers.TryAddWithoutValidation(header.Key, header.Value?.GetValue<string>());
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

		private StreamingToolArgumentsAnalysisResult DownloadFileStream(
			string? url, string? savePath)
		{
			return new StreamingToolArgumentsAnalysisResult
			{
				StatusIcon = MaterialIconKind.Download,
				StatusTitle = $"`{url}` → `{savePath}`"
			};
		}

		private async Task<ReactiveToolResult> DownloadFile(
			[Description("URL of the file to download")] string url,
			[Description("Local working directory path to save the file")] string savePath,
			[Description("Optional: Additional headers as JSON")] JsonObject? headersJson = null,
			CancellationToken cancellationToken = default)
		{
			var result = new ReactiveToolResult
			{
				StatusIcon = MaterialIconKind.Download,
				StatusTitle = $"`{url}` → `{savePath}`"
			};

			_ = Task.Run(async () =>
			{
				long downloadedBytes = 0;
				long? totalBytes = null;
				try
				{
					var fullSavePath = _fileAccess.AccessPath(savePath);
					using var fileStream = File.Create(fullSavePath);
					using var request = new HttpRequestMessage(HttpMethod.Get, url);

					if (headersJson != null)
					{
						foreach (var header in headersJson)
						{
							request.Headers.TryAddWithoutValidation(header.Key, header.Value?.GetValue<string>());
						}
					}

					var response = await _httpInfiniteTimeoutClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
					if (response.IsSuccessStatusCode)
					{
						using var downloadStream = await response.Content.ReadAsStreamAsync(cancellationToken);
						totalBytes = response.Content.Headers.ContentLength;
						if (!totalBytes.HasValue)
							try
							{
								totalBytes = downloadStream.Length;
							}
							catch { }

						int bytesRead;
						byte[] buffer = new byte[16384];

						while ((bytesRead = await downloadStream.ReadAsync(buffer, cancellationToken)) > 0)
						{
							fileStream.Write(buffer, 0, bytesRead);
							await fileStream.FlushAsync();
							downloadedBytes += bytesRead;

							if (totalBytes.HasValue)
							{
								double progress = (double)downloadedBytes / totalBytes.Value;
								result.StatusTitle = $"`{url}` → `{savePath}` " +
									$"({FileUtils.BytesToDisplaySize(downloadedBytes)} / " +
									$"{FileUtils.BytesToDisplaySize(totalBytes.Value)}, {progress:P2})";
								result.Progress = progress;
							}
							else
							{
								result.StatusTitle = $"`{url}` → `{savePath}` " +
									$"({FileUtils.BytesToDisplaySize(downloadedBytes)})";
								result.Progress = null;
							}
						}
					}

					result.ResultContent = $"""
						Status code: {(int)response.StatusCode}
						Status description: {response.StatusCode.ToString()}
						Downloaded: {downloadedBytes} ({FileUtils.BytesToDisplaySize(downloadedBytes)})
						Total size: {totalBytes ?? -1} {(totalBytes.HasValue ? FileUtils.BytesToDisplaySize(totalBytes.Value) : "unknown")}.
						""";
					result.CompleteWithSuccess();
				}
				catch (Exception ex)
				{
					result.StatusIcon = MaterialIconKind.DownloadOff;
					result.ResultContent = $"Error downloading file: {ex.Message}; " +
						$"downloaded ({FileUtils.BytesToDisplaySize(downloadedBytes)}) " +
						$"of {totalBytes ?? -1} ({(totalBytes.HasValue ? FileUtils.BytesToDisplaySize(totalBytes.Value) : "unknown")}).";
					result.CompleteWithError();
				}
			});
			return result;
		}

		private async Task<ToolResult> CheckWebsiteStatus(
			[Description("URL to check")] string url,
			[Description("Timeout in seconds (default: 30)")] int timeoutSeconds = 30,
			CancellationToken cancellationToken = default)
		{
			try
			{
				using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
				using var cts2 = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts1.Token);
				var startTime = DateTime.UtcNow;

				var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts2.Token);
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

		private static readonly ReverseMarkdown.Converter _mdConverter = new(
			new ReverseMarkdown.Config
			{
				UnknownTags = ReverseMarkdown.Config.UnknownTagsOption.Drop,
				RemoveComments = true,
				GithubFlavored = true,
				Base64Images = ReverseMarkdown.Config.Base64ImageHandling.Skip
			});

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

		private async Task<ToolResult> Fetch(
			[Description("URL to fetch HTML from")]
			string url,
			[Description("The starting index of character to return")]
			int start = 0,
			[Description("The maximum count of characters to return")]
			int count = 5000,
			[Description("The content type to fetch")]
			[Enum(["html", "sanitized_html", "markdown"])]
			string contentType = "markdown",
			CancellationToken cancellationToken = default)
		{
			try
			{
				if (start < 0)
					return new ToolResult(ToolResultStatus.Error,
						$"Error: 'start' parameter cannot be negative. Provided value: {start}");

				if (count <= 0)
					return new ToolResult(ToolResultStatus.Error,
						$"Error: 'count' parameter must be greater than 0. Provided value: {count}");

				var content = await _fetchContentCache.GetAsync((url, contentType), cancellationToken);

				if (start >= content.Length)
					return new ToolResult(ToolResultStatus.Error,
						$"Error: Start index {start} exceeds content length {content.Length}");

				var actualCount = Math.Min(count, content.Length - start);

				var slice = content.Substring(start, actualCount);

				var result = $"""
					Url: {url}
					Total content length: {content.Length}
					Showing slice: {start}-{start + actualCount}
					Content slice:
					{slice}
					""";
				return new ToolResult(result);
			}
			catch (Exception ex)
			{
				return new ToolResult(ToolResultStatus.Error, $"Error fetching web content: {ex.Message}");
			}
		}
		
		private async Task<ToolResult> ParseHtml(
			[Description("URL to fetch HTML from")] string url,
			[Description("The query selector to select values with")] string selector,
			CancellationToken cancellationToken = default)
		{
			try
			{
				var config = Configuration.Default.WithDefaultLoader();
				var context = BrowsingContext.New(config);
				var document = await context.OpenAsync(url, cancellationToken);
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

		protected override void Dispose(bool disposing)
		{
			_httpClient?.Dispose();
		}
	}
}