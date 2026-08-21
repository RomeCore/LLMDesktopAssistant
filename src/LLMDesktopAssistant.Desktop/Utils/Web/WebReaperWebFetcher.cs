using System.Net.Http;
using LLMDesktopAssistant.Utils.Web;
using Microsoft.Extensions.Logging;
using WebReaper.Cdp;
using WebReaper.Core.CookieStorage.Abstract;
using WebReaper.Core.Loaders.Abstract;
using WebReaper.Domain.Selectors;
using WebReaper.Playwright;
using WebReaper.Stealth.CloakBrowser;

namespace LLMDesktopAssistant.Desktop.Utils.Web;

/// <summary>
/// An <see cref="IWebFetcher"/> backed by WebReaper page-load transports with
/// three fetch modes: a plain HTTP transport, a headless Playwright browser
/// (renders JavaScript), and the stealth CloakBrowser (evades advanced
/// anti-bot systems). Browser transports are launched lazily on first use and
/// disposed with the fetcher.
/// </summary>
public sealed class WebReaperWebFetcher : IWebFetcher, IAsyncDisposable
{
	private readonly IPageLoadTransport _httpTransport;
	private readonly Lazy<IPageLoadTransport> _browserTransport;
	private readonly Lazy<Task<IPageLoadTransport>> _stealthTransport;
	private readonly ILogger _logger;
	private LaunchedCdpEndpoint? _stealthEndpoint;

	/// <summary>
	/// Creates a fetcher with an HTTP transport and lazy Playwright and
	/// CloakBrowser transports.
	/// </summary>
	/// <param name="logger">The logger.</param>
	public WebReaperWebFetcher(ILogger logger)
	{
		_logger = logger;
		ICookiesStorage cookies = new InMemoryCookieStorage();
		_httpTransport = new HttpPageLoadTransport(cookies, logger);
		_browserTransport = new Lazy<IPageLoadTransport>(() =>
			new PlaywrightPageLoadTransport(
				PlaywrightBrowser.Chromium,
				new PlaywrightLaunchOptions(),
				cookies,
				null,
				logger,
				new NullActionResolver()));
		_stealthTransport = new Lazy<Task<IPageLoadTransport>>(async () =>
		{
			var options = new CloakBrowserOptions { AutoInstall = AutoInstallPolicy.Disabled };
			var binaryPath = await CloakBrowserInstaller.EnsureInstalledAsync(options, logger, CancellationToken.None);
			var endpoint = await CloakBrowserLauncher.LaunchAsync(binaryPath, options, logger, CancellationToken.None);
			_stealthEndpoint = endpoint;
			return new CdpPageLoadTransport(endpoint.CdpUrl, cookies, null, logger, new NullActionResolver());
		});
	}

	/// <inheritdoc />
	public bool SupportsBrowser => true;

	/// <inheritdoc />
	public bool SupportsStealthBrowser => true;

	/// <inheritdoc />
	public async Task<string> FetchContentAsync(string url, WebFetchOptions? options = null, CancellationToken cancellationToken = default)
	{
		var result = await FetchWithMetadataAsync(url, options, cancellationToken);
		if (result.HttpStatus is >= 400)
			throw new HttpRequestException($"HTTP {result.HttpStatus} loading {url}.");
		return result.Html;
	}

	/// <inheritdoc />
	public async Task<FetchResult> FetchWithMetadataAsync(string url, WebFetchOptions? options = null, CancellationToken cancellationToken = default)
	{
		var useBrowser = options?.UseBrowser ?? false;
		var useStealthBrowser = options?.UseStealthBrowser ?? false;
		var request = new PageRequest(url, (useBrowser || useStealthBrowser) ? PageType.Dynamic : PageType.Static);

		IPageLoadTransport transport;
		if (useStealthBrowser)
			transport = await _stealthTransport.Value;
		else if (useBrowser)
			transport = _browserTransport.Value;
		else
			transport = _httpTransport;

		var result = await transport.LoadAsync(request, cancellationToken);
		return new FetchResult(result.Html, result.HttpStatus, result.Headers);
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (_browserTransport.IsValueCreated && _browserTransport.Value is IAsyncDisposable browserDisposable)
			await browserDisposable.DisposeAsync();

		if (_stealthTransport.IsValueCreated)
		{
			try
			{
				if (await _stealthTransport.Value is IAsyncDisposable stealthDisposable)
					await stealthDisposable.DisposeAsync();
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "WebReaperWebFetcher: failed to dispose the stealth transport.");
			}
		}

		if (_stealthEndpoint is not null)
			await _stealthEndpoint.DisposeAsync();
	}
}
