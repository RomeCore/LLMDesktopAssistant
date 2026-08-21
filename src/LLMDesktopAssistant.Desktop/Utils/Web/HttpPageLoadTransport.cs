using System.Net.Http;
using System.Net.Security;
using System.Text;
using Microsoft.Extensions.Logging;
using WebReaper.Core.CookieStorage.Abstract;
using WebReaper.Core.Loaders.Abstract;

namespace LLMDesktopAssistant.Desktop.Utils.Web;

/// <summary>
/// The HTTP <see cref="IPageLoadTransport"/> used by <see cref="WebReaperWebFetcher"/>:
/// a per-request <see cref="HttpClient"/> with cookie support, SSL certificate
/// bypass, and a browser-like User-Agent. A completed response with any status
/// code is returned as a <see cref="PageLoadResult"/>; only a genuine
/// no-response failure (DNS, connection refused, TLS error, timeout) is
/// surfaced as a <see cref="PageLoadException"/>.
/// </summary>
internal sealed class HttpPageLoadTransport : IPageLoadTransport
{
	private const string UserAgent =
		"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

	private readonly ICookiesStorage _cookiesStorage;
	private readonly ILogger _logger;

	public HttpPageLoadTransport(ICookiesStorage cookiesStorage, ILogger logger)
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		_cookiesStorage = cookiesStorage;
		_logger = logger;
	}

	public async Task<PageLoadResult> LoadAsync(PageRequest request, CancellationToken cancellationToken = default)
	{
		var cookies = await _cookiesStorage.GetAsync();

		var handler = new SocketsHttpHandler
		{
			MaxConnectionsPerServer = 100,
			PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
			PooledConnectionLifetime = Timeout.InfiniteTimeSpan,
			UseCookies = true,
			CookieContainer = cookies,
			AutomaticDecompression = System.Net.DecompressionMethods.All,
			SslOptions = new SslClientAuthenticationOptions
			{
				// Leave certs unvalidated for parity with the removed requesters.
				RemoteCertificateValidationCallback = delegate { return true; }
			},
		};

		using var client = new HttpClient(handler)
		{
			Timeout = TimeSpan.FromSeconds(30),
		};
		client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
		client.DefaultRequestHeaders.Add("Accept",
			"text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
		client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
		client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");

		HttpResponseMessage response;
		try
		{
			response = await client.GetAsync(request.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		}
		catch (HttpRequestException ex)
		{
			_logger.LogError(ex, "No response loading page {Url}", request.Url);
			throw new PageLoadException($"No response from {request.Url}: {ex.Message}", ex);
		}
		catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
		{
			throw new PageLoadException($"Timed out loading {request.Url}.", ex);
		}

		using (response)
		{
			var body = await response.Content.ReadAsStringAsync(cancellationToken);
			if (!response.IsSuccessStatusCode)
				_logger.LogWarning("Page {Url} returned status {StatusCode}", request.Url, (int)response.StatusCode);

			return new PageLoadResult
			{
				Html = body,
				HttpStatus = (int)response.StatusCode,
				Headers = CollectHeaders(response),
			};
		}
	}

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
