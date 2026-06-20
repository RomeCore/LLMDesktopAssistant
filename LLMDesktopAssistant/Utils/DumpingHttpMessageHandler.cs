using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Utils
{
	/// <summary>
	/// An <see cref="HttpMessageHandler"/> that intercepts all HTTP requests and responses,
	/// serializes them to JSON and writes them to <see cref="Directories.LocalAppData"/>/http-dumps/.
	/// <para>Useful for debugging HTTP calls made by LLM providers, MCP servers, etc.</para>
	/// </summary>
	public class DumpingHttpMessageHandler : DelegatingHandler
	{
		private readonly string _dumpDirectory;
		private static long _fileCounter;

		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			WriteIndented = true,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		};

		/// <summary>
		/// Initializes a new instance of <see cref="DumpingHttpMessageHandler"/>
		/// with the specified inner handler.
		/// </summary>
		public DumpingHttpMessageHandler(HttpMessageHandler innerHandler)
			: base(innerHandler ?? new HttpClientHandler())
		{
			_dumpDirectory = Path.Combine(Directories.LocalAppData, "http-dumps");
			Directory.CreateDirectory(_dumpDirectory);
		}

		/// <summary>
		/// Initializes a new instance with a default <see cref="HttpClientHandler"/>.
		/// </summary>
		public DumpingHttpMessageHandler()
			: this(new HttpClientHandler())
		{
		}

		/// <summary>
		/// The maximum number of dump files to keep. Older files are deleted when this limit is exceeded.
		/// Set to <c>0</c> to disable quota limit. Default: <c>500</c>.
		/// </summary>
		public int MaxDumpFiles { get; set; } = 500;

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var dump = new HttpDumpRecord
			{
				Timestamp = DateTime.UtcNow,
				Request = await CaptureRequestAsync(request, cancellationToken)
			};

			HttpResponseMessage response;
			try
			{
				response = await base.SendAsync(request, cancellationToken);
			}
			catch (Exception ex)
			{
				dump.Response = new HttpResponseDump
				{
					StatusCode = 0,
					ReasonPhrase = "Exception",
					Body = ex.ToString(),
					Error = ex.GetType().Name
				};
				await TryWriteDumpAsync(dump);
				throw;
			}

			dump.Response = await CaptureResponseAsync(response, cancellationToken);
			await TryWriteDumpAsync(dump);

			return response;
		}

		private static async Task<HttpRequestDump> CaptureRequestAsync(HttpRequestMessage request, CancellationToken ct)
		{
			var dump = new HttpRequestDump
			{
				Method = request.Method.ToString(),
				Url = request.RequestUri?.ToString(),
				Headers = request.Headers.ToDictionary(
					h => h.Key, h => h.Value.ToList(), StringComparer.OrdinalIgnoreCase)
			};

			if (request.Content != null)
			{
				dump.ContentHeaders = request.Content.Headers.ToDictionary(
					h => h.Key, h => h.Value.ToList(), StringComparer.OrdinalIgnoreCase);

				var bodyBytes = await request.Content.ReadAsByteArrayAsync(ct);
				dump.Body = Encoding.UTF8.GetString(bodyBytes);

				// Restore the content for downstream handlers
				request.Content = new ByteArrayContent(bodyBytes);
				if (dump.ContentHeaders is { Count: > 0 })
				{
					foreach (var (key, values) in dump.ContentHeaders)
						request.Content.Headers.TryAddWithoutValidation(key, values);
				}
			}

			return dump;
		}

		private static async Task<HttpResponseDump> CaptureResponseAsync(HttpResponseMessage response, CancellationToken ct)
		{
			var dump = new HttpResponseDump
			{
				StatusCode = (int)response.StatusCode,
				ReasonPhrase = response.ReasonPhrase,
				Headers = response.Headers.ToDictionary(
					h => h.Key, h => h.Value.ToList(), StringComparer.OrdinalIgnoreCase)
			};

			if (response.Content != null)
			{
				dump.ContentHeaders = response.Content.Headers.ToDictionary(
					h => h.Key, h => h.Value.ToList(), StringComparer.OrdinalIgnoreCase);

				var bodyBytes = await response.Content.ReadAsByteArrayAsync(ct);
				dump.Body = Encoding.UTF8.GetString(bodyBytes);

				// Replace the content so the caller can still read it
				response.Content = new ByteArrayContent(bodyBytes);
				if (dump.ContentHeaders is { Count: > 0 })
				{
					foreach (var (key, values) in dump.ContentHeaders)
						response.Content.Headers.TryAddWithoutValidation(key, values);
				}
			}

			return dump;
		}

		private async Task TryWriteDumpAsync(HttpDumpRecord dump)
		{
			try
			{
				var seq = Interlocked.Increment(ref _fileCounter);
				var fileName = $"http_dump_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss.fff}_{seq:D4}.json";
				var filePath = Path.Combine(_dumpDirectory, fileName);

				var json = JsonSerializer.Serialize(dump, JsonOptions);
				await File.WriteAllTextAsync(filePath, json);

				EnforceDumpQuota();
			}
			catch
			{
				// Logging failure MUST never crash the actual HTTP call
			}
		}

		private void EnforceDumpQuota()
		{
			if (MaxDumpFiles <= 0)
				return;

			try
			{
				var files = Directory.EnumerateFiles(_dumpDirectory, "http_dump_*.json")
					.OrderByDescending(f => f)
					.Skip(MaxDumpFiles)
					.ToArray();

				foreach (var file in files)
				{
					try { File.Delete(file); }
					catch { /* best effort */ }
				}
			}
			catch
			{
				// Best effort
			}
		}

		#region DTO classes

		public class HttpDumpRecord
		{
			public DateTime Timestamp { get; set; }
			public HttpRequestDump Request { get; set; } = new();
			public HttpResponseDump? Response { get; set; }
		}

		public class HttpRequestDump
		{
			public string Method { get; set; } = string.Empty;
			public string? Url { get; set; }
			public Dictionary<string, List<string>>? Headers { get; set; }
			public Dictionary<string, List<string>>? ContentHeaders { get; set; }
			public string? Body { get; set; }
		}

		public class HttpResponseDump
		{
			public int StatusCode { get; set; }
			public string? ReasonPhrase { get; set; }
			public Dictionary<string, List<string>>? Headers { get; set; }
			public Dictionary<string, List<string>>? ContentHeaders { get; set; }
			public string? Body { get; set; }
			public string? Error { get; set; }
		}

		#endregion
	}
}
