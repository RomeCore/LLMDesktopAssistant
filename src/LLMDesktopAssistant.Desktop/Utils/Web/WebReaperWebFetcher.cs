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
	public WebFetchLevel MaxLevel => WebFetchLevel.StealthBrowser;

	/// <inheritdoc />
	public async Task<FetchResult> FetchAsync(string url, WebFetchLevel minFetchLevel = WebFetchLevel.HttpClient, CancellationToken cancellationToken = default)
	{
		var level = minFetchLevel > MaxLevel ? MaxLevel : minFetchLevel;

		IPageLoadTransport transport = level switch
		{
			WebFetchLevel.Browser => _browserTransport.Value,
			WebFetchLevel.StealthBrowser => await _stealthTransport.Value,
			_ => _httpTransport
		};

		var request = new PageRequest(url, level == WebFetchLevel.HttpClient ? PageType.Static : PageType.Dynamic);
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
