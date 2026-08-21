using LLMDesktopAssistant.Settings.Application;
using Microsoft.Extensions.Logging;

namespace LLMDesktopAssistant.Utils.Web;

/// <summary>
/// A decorator over <see cref="IWebFetcher"/> that resolves the effective
/// <see cref="WebFetchLevel"/> from the <see cref="WebFetchSettings"/> (the
/// default fetch level and the enabled levels), then automatically retries
/// the fetch at higher levels when the page denies access (for example,
/// HTTP 401, 403, 407, 429, or 451). Escalation stops at the lower of the
/// settings-enabled level and the inner fetcher's <see cref="IWebFetcher.MaxLevel"/>.
/// </summary>
public sealed class AutoEscalatingWebFetcher : IWebFetcher
{
	private readonly IWebFetcher _inner;
	private readonly ILogger _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="AutoEscalatingWebFetcher"/> class.
	/// </summary>
	/// <param name="inner">The fetcher to delegate to.</param>
	/// <param name="logger">The logger.</param>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="inner"/> or <paramref name="logger"/> is <see langword="null"/>.
	/// </exception>
	public AutoEscalatingWebFetcher(IWebFetcher inner, ILogger logger)
	{
		_inner = inner ?? throw new ArgumentNullException(nameof(inner));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <inheritdoc />
	public WebFetchLevel MaxLevel => _inner.MaxLevel;

	/// <inheritdoc />
	public async Task<FetchResult> FetchAsync(string url, WebFetchLevel minFetchLevel = WebFetchLevel.HttpClient, CancellationToken cancellationToken = default)
	{
		var settings = ApplicationSettingsAccessor.ApplicationSettings.WebFetch;
		var settingsMax = settings.EnableStealthBrowser ? WebFetchLevel.StealthBrowser
			: settings.EnableBrowser ? WebFetchLevel.Browser
			: WebFetchLevel.HttpClient;
		var maxLevel = MaxLevel < settingsMax ? MaxLevel : settingsMax;

		var level = minFetchLevel > settings.DefaultFetchLevel ? minFetchLevel : settings.DefaultFetchLevel;
		if (level > maxLevel)
			level = maxLevel;

		var result = await _inner.FetchAsync(url, level, cancellationToken);

		while (level < maxLevel && IsAccessDenied(result.HttpStatus))
		{
			var previousLevel = level;
			level++;
			_logger.LogWarning(
				"Escalating web fetch level for {Url} from {PreviousLevel} to {Level} after status {HttpStatus}",
				url, previousLevel, level, result.HttpStatus);
			result = await _inner.FetchAsync(url, level, cancellationToken);
		}

		return result;
	}

	/// <summary>
	/// Determines whether an HTTP status code indicates that the page denied
	/// access and a higher <see cref="WebFetchLevel"/> might help.
	/// </summary>
	/// <param name="statusCode">The HTTP status code, or <see langword="null"/> if unknown.</param>
	/// <returns><see langword="true"/> if the status code is an access-denied error; otherwise, <see langword="false"/>.</returns>
	public static bool IsAccessDenied(int? statusCode)
		=> statusCode is 401 or 403 or 407 or 429 or 451;
}
