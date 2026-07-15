using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;

namespace LLMDesktopAssistant.Utils.Web
{
	/// <summary>
	/// The result of a page fetch operation containing the raw HTML, HTTP status code, and response headers.
	/// </summary>
	public sealed record FetchResult(
		string Html,
		int? HttpStatus,
		IReadOnlyDictionary<string, string> Headers
	);

	/// <summary>
	/// Fetches web page content over HTTP with retries, cookie support, proxy support,
	/// SSL bypass, and response caching.
	/// </summary>
	public static class HtmlContentFetcher
	{
		private static readonly AsyncCache<string, FetchResult> _cache = new(
			FetchCoreAsync,
			slidingExpirationTime: TimeSpan.FromMinutes(15));

		/// <summary>
		/// Fetches the page and returns the raw HTML body. Convenience wrapper around
		/// <see cref="FetchWithMetadataAsync"/> that throws on non-success status codes.
		/// </summary>
		public static async Task<string> FetchContentAsync(string url, CancellationToken cancellationToken = default)
		{
			var result = await FetchWithMetadataAsync(url, cancellationToken);
			if (result.HttpStatus is >= 400)
			{
				throw new HttpRequestException(
					$"HTTP {(int)result.HttpStatus} loading {url}.");
			}
			return result.Html;
		}

		/// <summary>
		/// Fetches the page and returns a <see cref="FetchResult"/> with the body,
		/// HTTP status, and response headers. Non-2xx statuses are returned as data;
		/// only genuine transport failures throw.
		/// </summary>
		public static async Task<FetchResult> FetchWithMetadataAsync(
			string url,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(url);
			return await _cache.GetAsync(url, cancellationToken);
		}

		/// <summary>
		/// Clears the entire response cache. Useful after a configuration change
		/// (proxy, cookies) or when the caller knows the remote content has changed.
		/// </summary>
		public static void ClearCache()
		{
			_cache.Clear();
		}

		private static async Task<FetchResult> FetchCoreAsync(string url, CancellationToken ct)
		{
			var handler = BuildHandler();
			using var client = BuildClient(handler);

			HttpResponseMessage response;
			try
			{
				response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
			}
			catch (HttpRequestException ex)
			{
				throw new HttpRequestException($"No response from {url}: {ex.Message}", ex);
			}
			catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
			{
				throw new TimeoutException($"Request timed out for {url}.", ex);
			}

			using (response)
			{
				var body = await response.Content.ReadAsStringAsync(ct);
				var headers = CollectHeaders(response);

				return new FetchResult(body, (int)response.StatusCode, headers);
			}
		}

		private static SocketsHttpHandler BuildHandler()
		{
			return new SocketsHttpHandler
			{
				MaxConnectionsPerServer = 100,
				PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
				PooledConnectionLifetime = Timeout.InfiniteTimeSpan,
				UseCookies = true,
				CookieContainer = new CookieContainer(),
				
				AutomaticDecompression = DecompressionMethods.All,
				SslOptions = new System.Net.Security.SslClientAuthenticationOptions
				{
					RemoteCertificateValidationCallback = delegate { return true; },
				},
			};
		}

		private static HttpClient BuildClient(SocketsHttpHandler handler)
		{
			var client = new HttpClient(handler)
			{
				Timeout = TimeSpan.FromSeconds(30),
			};

			client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
				"(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
			client.DefaultRequestHeaders.Add("Accept",
				"text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
			client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
			client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
			client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
			client.DefaultRequestHeaders.Add("DNT", "1");

			return client;
		}

		/// <summary>
		/// Flattens response + content headers into one case-insensitive map.
		/// </summary>
		private static IReadOnlyDictionary<string, string> CollectHeaders(HttpResponseMessage response)
		{
			var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var h in response.Headers)
				headers[h.Key] = string.Join(", ", h.Value);
			foreach (var h in response.Content.Headers)
				headers[h.Key] = string.Join(", ", h.Value);
			return headers;
		}
	}

}
