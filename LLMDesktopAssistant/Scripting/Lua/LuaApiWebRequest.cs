using System.Text;
using AngleSharp;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Serialization.Json;
using ReverseMarkdown;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Lua API for HTTP/web operations: <c>web.request()</c>, <c>web.fetch()</c>, <c>web.status()</c>, <c>web.parse()</c>, <c>web.download()</c>.
	/// Registered in the global <c>web</c> namespace alongside <c>web.search()</c>.
	/// </summary>
	[LuaApi(chatScoped: true)]
	public class LuaApiWebRequest : LuaApiBase
	{
		public override string? Namespace => "web";

		public override string? Manuals => """
			--- web — web request, fetch, parse and download API

			Provides HTTP requests, content fetching (HTML → Markdown),
			website status checking, HTML parsing with CSS selectors, and file downloads.

			FUNCTIONS:

			--- web.request(method, url, [params])
			  Performs an HTTP request with the specified method and URL.
			  Returns structured response data.

			  Parameters:
			    - method: string — HTTP method: "GET", "POST", "PUT", "DELETE"
			    - url: string — The request URL
			    - params: table (optional) — Additional parameters:
			      - use_pure_client: boolean (default: false) — If true, use http client without additional headers
			      - headers: table (optional) — Custom headers as key-value pairs
			      - content: string or table (optional) — Request body, if table it's serialized to JSON
			      - contentType: string (default: "application/json") — Content-Type header

			  Returns: table with:
			    - status_code: number — HTTP status code (e.g. 200)
			    - status_description: string — Status description (e.g. "OK")
			    - headers: table — Response headers as key-value pairs
			    - content_length: number — Length of the response content
			    - content: string — Response body (sliced by start/count)
			
			--- web.status(url, [options])
			  Checks if a website is accessible and returns detailed status info.
			
			  Parameters:
			    - url: string — URL to check
			    - options: table (optional):
			      - timeout: number (default: 30) — Timeout in seconds
			
			  Returns: table with:
			    - url: string — the checked URL
			    - status_code: number — HTTP status code
			    - status_description: string — Status description
			    - is_accessible: boolean — Whether the site is reachable
			    - response_time_ms: number — Response time in milliseconds
			    - content_type: string or nil — Content-Type header
			    - content_length: number or nil — Content-Length header
			    - server: string or nil — Server header
			
			--- web.fetch(url, [contentType])
			  Fetches a web page and converts it to the specified content type.

			  Parameters:
			    - url: string — URL to fetch
			    - contentType: string (default: "markdown") — "html", "sanitized_html", or "markdown"

			  Returns: table with:
			    - url: string — the fetched URL
			    - content_type: string — the actual content type returned
			    - content_length: number — total length of fetched content
			    - content: string — the converted content (sliced by start/count)

			--- web.parse(url, selector)
			  Fetches HTML content and parses specific elements using a CSS selector.

			  Parameters:
			    - url: string — URL to fetch HTML from
			    - selector: string — CSS selector (e.g. "h1", ".content", "#main > p")

			  Returns: table with:
			    - url: string — the fetched URL
			    - selector: string — the CSS selector used
			    - match_count: number — number of matched elements
			    - contents: array of strings — text content of each matched element
			    - html: string — combined HTML of matched elements (sliced)

			--- web.download(url, savePath, [options])
			  Downloads a file from a URL and saves it to the local filesystem.

			  Parameters:
			    - url: string — URL of the file to download
			    - savePath: string — Path to save the file (relative to the working directory)
			    - options: table (optional):
			      - headers: table (optional) — Custom headers as key-value pairs

			  Returns: table with:
			    - url: string — source URL
			    - file_size: number — downloaded file size in bytes
			    - status_code: number — HTTP status code
			    - success: boolean — whether the download succeeded

			EXAMPLES:

			  -- Fetch a page as Markdown
			  local r = web.fetch("https://example.com")
			  print(r.content)

			  -- Fetch as raw HTML
			  local r = web.fetch("https://example.com", { contentType = "html" })
			  print(r.content)

			  -- Check website status
			  local s = web.status("https://google.com")
			  print(s.status_code, s.response_time_ms)

			  -- Parse specific elements
			  local r = web.parse("https://news.ycombinator.com", "a.storylink")
			  for _, title in ipairs(r.contents) do
			    print(title)
			  end

			  -- Download a file
			  local r = web.download("https://example.com/image.png", "downloaded_image.png")
			  print("Saved", r.file_size, "bytes to", r.save_path)

			  -- Request with custom headers and content slicing
			  local r = web.request("POST", "https://api.example.com/data", {
			    headers = { ["Authorization"] = "Bearer token" },
			    content = { key = "value" },
			    contentType = "application/json"
			  })
			  print(r.status_code, r.content)
			""";

		private readonly FileAccessService _fileAccess;
		private readonly HttpClient _httpClient, _pureHttpClient, _infiniteTimeoutClient;
		private static readonly Converter _mdConverter = new(
			new Config
			{
				UnknownTags = Config.UnknownTagsOption.Drop,
				RemoveComments = true,
				GithubFlavored = true,
				Base64Images = Config.Base64ImageHandling.Skip
			});

		public LuaApiWebRequest(FileAccessService fileAccess)
		{
			_fileAccess = fileAccess;
			_httpClient = CreateClient(timeoutSeconds: 30);
			_pureHttpClient = new HttpClient();
			_infiniteTimeoutClient = CreateClient(timeoutSeconds: null);
		}

		public override void Populate(Table globals, Table ns, LuaService luaService)
		{
			ns["request"] = DynValue.NewCallback(new CallbackFunction(Request));
			ns["status"] = DynValue.NewCallback(new CallbackFunction(Status));
			ns["fetch"] = DynValue.NewCallback(new CallbackFunction(Fetch));
			ns["parse"] = DynValue.NewCallback(new CallbackFunction(Parse));
			ns["download"] = DynValue.NewCallback(new CallbackFunction(Download));
		}

		private DynValue Request(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("web.request() requires at least method and url arguments.");

			var method = args[0].CastToString()
				?? throw new ScriptRuntimeException("First argument must be a string (HTTP method).");
			var url = args[1].CastToString()
				?? throw new ScriptRuntimeException("Second argument must be a string (URL).");

			// Defaults
			string? content = null;
			string contentType = "application/json";
			Dictionary<string, string>? headers = null;
			bool usePureClient = false;

			// Parse optional params table
			if (args.Count > 2 && args[2].Type == DataType.Table)
			{
				var opts = args[2].Table;

				if (opts.Get("use_pure_client") is DynValue upc && upc.Type == DataType.Boolean)
					usePureClient = upc.Boolean;

				if (opts.Get("content") is DynValue cv)
				{
					if (cv.Type == DataType.String)
						content = cv.String;
					else if (cv.Type == DataType.Table)
						content = cv.Table.TableToJson();
				}

				if (opts.Get("contentType") is DynValue ct && ct.Type == DataType.String)
					contentType = ct.String;

				if (opts.Get("headers") is DynValue hv && hv.Type == DataType.Table)
				{
					headers = new Dictionary<string, string>();
					foreach (var kv in hv.Table.Pairs)
					{
						var key = kv.Key.CastToString();
						var val = kv.Value.CastToString();
						if (key != null && val != null)
							headers[key] = val;
					}
				}
			}

			try
			{
				var httpMethod = method.ToUpperInvariant() switch
				{
					"GET" => HttpMethod.Get,
					"POST" => HttpMethod.Post,
					"PUT" => HttpMethod.Put,
					"DELETE" => HttpMethod.Delete,
					_ => throw new ScriptRuntimeException($"Invalid HTTP method: '{method}'. Supported: GET, POST, PUT, DELETE.")
				};

				using var request = new HttpRequestMessage(httpMethod, url);

				if (!string.IsNullOrEmpty(content))
					request.Content = new StringContent(content, Encoding.UTF8, contentType);

				if (headers != null)
				{
					foreach (var header in headers)
						request.Headers.TryAddWithoutValidation(header.Key, header.Value);
				}

				var client = usePureClient ? _pureHttpClient : _httpClient;
				var response = client.SendAsync(request).Result;
				var responseContent = response.Content.ReadAsStringAsync().Result;

				var script = ctx.OwnerScript;
				var result = new Table(script);

				result["status_code"] = DynValue.NewNumber((int)response.StatusCode);
				result["status_description"] = DynValue.NewString(response.StatusCode.ToString());

				var headersTable = new Table(script);
				foreach (var h in response.Headers)
					headersTable[h.Key] = DynValue.NewString(string.Join(", ", h.Value));
				foreach (var h in response.Content.Headers)
					headersTable[h.Key] = DynValue.NewString(string.Join(", ", h.Value));
				result["headers"] = headersTable;

				result["content_length"] = DynValue.NewNumber(responseContent.Length);
				result["content"] = DynValue.NewString(responseContent);

				return DynValue.NewTable(result);
			}
			catch (ScriptRuntimeException) { throw; }
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"HTTP request failed: {ex.Message}");
			}
		}

		private DynValue Fetch(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("web.fetch(url, [contentType]): at least 1 argument expected.");

			var url = args[0].CastToString()
				?? throw new ScriptRuntimeException("First argument must be a string (URL).");

			string contentType = "markdown";

			if (args.Count > 1 && args[1].Type == DataType.String)
			{
				contentType = args[1].String;
			}

			try
			{
				var config = Configuration.Default.WithDefaultLoader();
				var context = BrowsingContext.New(config);
				var document = context.OpenAsync(url).Result;

				var bodyHtml = document.Body?.OuterHtml ?? string.Empty;
				string content;

				switch (contentType)
				{
					case "sanitized_html":
						content = HtmlUtils.Sanitize(bodyHtml);
						break;

					case "markdown":
						content = _mdConverter.Convert(bodyHtml);
						break;

					case "html":
					default:
						content = bodyHtml;
						break;
				}

				var script = ctx.OwnerScript;
				var result = new Table(script);

				result["url"] = DynValue.NewString(url);
				result["content_type"] = DynValue.NewString(contentType);
				result["content_length"] = DynValue.NewNumber(content.Length);
				result["content"] = DynValue.NewString(content);

				return DynValue.NewTable(result);
			}
			catch (ScriptRuntimeException) { throw; }
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"Web fetch failed: {ex.Message}");
			}
		}

		private DynValue Status(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("web.status(url, [options]): at least 1 argument expected.");

			var url = args[0].CastToString()
				?? throw new ScriptRuntimeException("First argument must be a string (URL).");

			int timeout = 30;
			if (args.Count > 1 && args[1].Type == DataType.Table)
			{
				var opts = args[1].Table;
				if (opts.Get("timeout") is DynValue tv && tv.Type == DataType.Number)
					timeout = (int)tv.Number;
			}

			try
			{
				using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
				var startTime = DateTime.UtcNow;

				var response = _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token).Result;
				var endTime = DateTime.UtcNow;

				var script = ctx.OwnerScript;
				var result = new Table(script);

				result["url"] = DynValue.NewString(url);
				result["status_code"] = DynValue.NewNumber((int)response.StatusCode);
				result["status_description"] = DynValue.NewString(response.StatusCode.ToString());
				result["is_accessible"] = DynValue.NewBoolean(response.IsSuccessStatusCode);
				result["response_time_ms"] = DynValue.NewNumber(Math.Round((endTime - startTime).TotalMilliseconds, 2));
				result["content_type"] = DynValue.NewString(response.Content.Headers.ContentType?.ToString());
				result["content_length"] = DynValue.NewNumber(response.Content.Headers.ContentLength ?? -1);
				result["server"] = DynValue.NewString(response.Headers.Server?.ToString() ?? "Unknown");

				return DynValue.NewTable(result);
			}
			catch (TaskCanceledException)
			{
				var script = ctx.OwnerScript;
				var result = new Table(script);
				result["url"] = DynValue.NewString(url);
				result["status_code"] = DynValue.NewNumber(0);
				result["status_description"] = DynValue.NewString("Timeout");
				result["is_accessible"] = DynValue.False;
				result["response_time_ms"] = DynValue.NewNumber(timeout * 1000);
				result["content_type"] = DynValue.Nil;
				result["content_length"] = DynValue.NewNumber(-1);
				result["server"] = DynValue.Nil;
				return DynValue.NewTable(result);
			}
			catch (ScriptRuntimeException) { throw; }
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"Website status check failed: {ex.Message}");
			}
		}

		private DynValue Parse(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("web.parse(url, selector): at least 2 arguments expected.");

			var url = args[0].CastToString()
				?? throw new ScriptRuntimeException("First argument must be a string (URL).");
			var selector = args[1].CastToString()
				?? throw new ScriptRuntimeException("Second argument must be a string (CSS selector).");

			try
			{
				var config = Configuration.Default.WithDefaultLoader();
				var context = BrowsingContext.New(config);
				var document = context.OpenAsync(url).Result;
				var elements = document.QuerySelectorAll(selector);

				var script = ctx.OwnerScript;
				var result = new Table(script);

				result["url"] = DynValue.NewString(url);
				result["selector"] = DynValue.NewString(selector);

				var matchCount = elements.Length;
				result["match_count"] = DynValue.NewNumber(matchCount);

				// Contents array
				var contentsTable = new Table(script);
				int i = 1;
				foreach (var el in elements)
				{
					contentsTable[i++] = DynValue.NewString(el.TextContent.Trim());
				}
				result["contents"] = contentsTable;

				// Combined HTML
				var combinedHtml = string.Join("\n\n", elements.Select(e => e.OuterHtml));
				result["html"] = DynValue.NewString(combinedHtml);

				return DynValue.NewTable(result);
			}
			catch (ScriptRuntimeException) { throw; }
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"HTML parse failed: {ex.Message}");
			}
		}

		private DynValue Download(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("web.download(url, savePath, [options]): at least 2 arguments expected.");

			var url = args[0].CastToString()
				?? throw new ScriptRuntimeException("First argument must be a string (URL).");
			var savePath = args[1].CastToString()
				?? throw new ScriptRuntimeException("Second argument must be a string (save path).");

			Dictionary<string, string>? headers = null;
			if (args.Count > 2 && args[2].Type == DataType.Table)
			{
				var opts = args[2].Table;
				if (opts.Get("headers") is DynValue hv && hv.Type == DataType.Table)
				{
					headers = new Dictionary<string, string>();
					foreach (var kv in hv.Table.Pairs)
					{
						var key = kv.Key.CastToString();
						var val = kv.Value.CastToString();
						if (key != null && val != null)
							headers[key] = val;
					}
				}
			}

			try
			{
				var fullSavePath = _fileAccess.AccessPath(savePath);

				// Ensure directory exists
				var dir = Path.GetDirectoryName(fullSavePath);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
					Directory.CreateDirectory(dir);

				using var request = new HttpRequestMessage(HttpMethod.Get, url);
				if (headers != null)
				{
					foreach (var header in headers)
						request.Headers.TryAddWithoutValidation(header.Key, header.Value);
				}

				var response = _infiniteTimeoutClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result;
				var script = ctx.OwnerScript;
				var result = new Table(script);

				if (!response.IsSuccessStatusCode)
				{
					result["url"] = DynValue.NewString(url);
					result["file_size"] = DynValue.NewNumber(0);
					result["status_code"] = DynValue.NewNumber((int)response.StatusCode);
					result["success"] = DynValue.False;
					return DynValue.NewTable(result);
				}

				using var downloadStream = response.Content.ReadAsStreamAsync().Result;
				using var fileStream = File.Create(fullSavePath);
				downloadStream.CopyTo(fileStream);
				var fileSize = fileStream.Length;

				result["url"] = DynValue.NewString(url);
				result["file_size"] = DynValue.NewNumber(fileSize);
				result["status_code"] = DynValue.NewNumber((int)response.StatusCode);
				result["success"] = DynValue.True;

				return DynValue.NewTable(result);
			}
			catch (ScriptRuntimeException) { throw; }
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"File download failed: {ex.Message}");
			}
		}

		private static HttpClient CreateClient(int? timeoutSeconds = 30)
		{
			var client = new HttpClient();
			client.DefaultRequestHeaders.Add("User-Agent",
				"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
			client.DefaultRequestHeaders.Add("Accept",
				"text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
			client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
			client.DefaultRequestHeaders.Add("Connection", "keep-alive");
			client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

			if (timeoutSeconds.HasValue)
				client.Timeout = TimeSpan.FromSeconds(timeoutSeconds.Value);
			else
				client.Timeout = Timeout.InfiniteTimeSpan;

			return client;
		}
	}
}
