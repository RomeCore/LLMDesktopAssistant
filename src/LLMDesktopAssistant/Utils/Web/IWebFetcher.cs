namespace LLMDesktopAssistant.Utils.Web;

/// <summary>
/// Options that control how a page is fetched by an <see cref="IWebFetcher"/>.
/// </summary>
/// <param name="UseBrowser">
/// Whether to load the page with a headless browser (rendering JavaScript)
/// instead of a plain HTTP request. Implementations that do not support
/// browser fetching ignore this flag.
/// </param>
/// <param name="UseStealthBrowser">
/// Whether to load the page with the stealth CloakBrowser, which evades
/// advanced anti-bot systems. Takes precedence over <paramref name="UseBrowser"/>.
/// Implementations that do not support stealth fetching ignore this flag.
/// </param>
public sealed record WebFetchOptions(bool UseBrowser = false, bool UseStealthBrowser = false);

/// <summary>
/// Fetches the raw HTML content of web pages. Implementations return the
/// page body exactly as received — no parsing, sanitization, or Markdown
/// conversion is performed. Conversion and parsing are the caller's
/// responsibility (for example with AngleSharp).
/// </summary>
public interface IWebFetcher
{
	/// <summary>
	/// Gets a value indicating whether this fetcher can load pages with a
	/// headless browser (rendering JavaScript).
	/// </summary>
	bool SupportsBrowser { get; }

	/// <summary>
	/// Gets a value indicating whether this fetcher can load pages with the
	/// stealth CloakBrowser.
	/// </summary>
	bool SupportsStealthBrowser { get; }

	/// <summary>
	/// Fetches the page and returns the raw HTML body.
	/// </summary>
	/// <param name="url">The URL to fetch.</param>
	/// <param name="options">Optional fetch options; <see langword="null"/> uses the defaults.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The raw HTML body of the page.</returns>
	/// <exception cref="HttpRequestException">Thrown when the server returns a status code of 400 or higher.</exception>
	Task<string> FetchContentAsync(string url, WebFetchOptions? options = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Fetches the page and returns a <see cref="FetchResult"/> with the body,
	/// HTTP status, and response headers. Non-success statuses are returned as
	/// data; only genuine transport failures throw.
	/// </summary>
	/// <param name="url">The URL to fetch.</param>
	/// <param name="options">Optional fetch options; <see langword="null"/> uses the defaults.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The <see cref="FetchResult"/> with the raw body and response metadata.</returns>
	Task<FetchResult> FetchWithMetadataAsync(string url, WebFetchOptions? options = null, CancellationToken cancellationToken = default);
}
