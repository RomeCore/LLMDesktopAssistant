using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Serialization.Json;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Lua API for HTTP requests: <c>web.request(method, url, params)</c>.
	/// Registered in the same <c>web</c> namespace as <c>web.search()</c>.
	/// </summary>
	[LuaApi]
	public class LuaApiWebRequest : LuaApiBase
	{
		private readonly HttpClient _httpClient, _pureHttpClient;

		public override string? Namespace => "web";

		public override string? Manuals => """
			--- web.request(method, url, [params]) — HTTP request API

			Performs an HTTP request with the specified method and URL.
			Returns structured response data.

			Parameters:
			  - method: string — HTTP method: "GET", "POST", "PUT", "DELETE"
			  - url: string — The request URL
			  - params: table (optional) — Additional parameters:
			    - use_pure_client: boolean (default: false) — If true, use http client without additional headers (such as User-Agent, Accept)
			    - headers: table (optional) — Custom headers as key-value pairs
			      e.g. { ["Authorization"] = "Bearer token", ["X-Custom"] = "value" }
			    - content: string or table (optional) — Request body content, if table is provided, it will be serialized to JSON
			    - contentType: string (default: "application/json") — Content-Type header

			Returns: table with:
			  - status_code: number — HTTP status code (e.g. 200)
			  - status_description: string — Status description (e.g. "OK")
			  - headers: table — Response headers as key-value pairs
			  - content_length: number — Length of the response content
			  - content: string — Response body

			EXAMPLES:

			  -- Simple GET
			  local r = web.request("GET", "https://api.example.com/data")
			  print(r.status_code, r.content)

			  -- POST with JSON body and custom headers
			  local r = web.request("POST", "https://api.example.com/submit", {
			    headers = {
			      ["Authorization"] = "Bearer mytoken",
			      ["X-Trace-Id"] = "12345"
			    },
			    content = '{"key": "value"}',
			    contentType = "application/json"
			  })
			  print(r.status_code, r.content)
			""";

		public LuaApiWebRequest()
		{
			_httpClient = new HttpClient();
			_httpClient.DefaultRequestHeaders.Add("User-Agent",
				"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
			_httpClient.DefaultRequestHeaders.Add("Accept",
				"text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
			_httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
			_httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
			_httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

			_pureHttpClient = new HttpClient();
		}

		public override void Populate(Table globals, Table ns)
		{
			ns["request"] = DynValue.NewCallback(new CallbackFunction(Request));
		}

		private DynValue Request(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("web.request() requires at least method and url arguments.");

			var method = args[0].CastToString();
			if (method == null)
				throw new ScriptRuntimeException("First argument must be a string (HTTP method).");

			var url = args[1].CastToString();
			if (url == null)
				throw new ScriptRuntimeException("Second argument must be a string (URL).");

			// Defaults
			string? content = null;
			string contentType = "application/json";
			Dictionary<string, string>? headers = null;
			bool usePureClient = false;

			// Parse optional params table
			if (args.Count > 2 && args[2].Type == DataType.Table)
			{
				var opts = args[2].Table;

				if (opts.Get("use_pure_client") is DynValue usePureClientVal && usePureClientVal.Type == DataType.Boolean)
					usePureClient = usePureClientVal.Boolean;

				if (opts.Get("content") is DynValue contentVal)
				{
					if (contentVal.Type == DataType.String)
						content = contentVal.String;
					else if (contentVal.Type == DataType.Table)
						content = contentVal.Table.TableToJson();
				}

				if (opts.Get("contentType") is DynValue ctVal && ctVal.Type == DataType.String)
					contentType = ctVal.String;

				if (opts.Get("headers") is DynValue headersVal && headersVal.Type == DataType.Table)
				{
					headers = new Dictionary<string, string>();
					foreach (var kv in headersVal.Table.Pairs)
					{
						var key = kv.Key.CastToString();
						var val = kv.Value.CastToString();
						if (key != null && val != null)
							headers[key] = val;
					}
				}
			}

			// Execute request
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

				// Build result table
				var script = ctx.OwnerScript;
				var result = new Table(script);

				result["status_code"] = DynValue.NewNumber((int)response.StatusCode);
				result["status_description"] = DynValue.NewString(response.StatusCode.ToString());

				// Response headers
				var headersTable = new Table(script);
				foreach (var h in response.Headers)
				{
					headersTable[h.Key] = DynValue.NewString(string.Join(", ", h.Value));
				}
				result["headers"] = headersTable;

				result["content_length"] = DynValue.NewNumber(responseContent.Length);
				result["content"] = DynValue.NewString(responseContent);

				return DynValue.NewTable(result);
			}
			catch (ScriptRuntimeException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"HTTP request failed: {ex.Message}");
			}
		}
	}
}
