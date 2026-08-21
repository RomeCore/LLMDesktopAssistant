namespace LLMDesktopAssistant.Utils.Web;

/// <summary>
/// An <see cref="IWebFetcher"/> backed by the <see cref="HtmlContentFetcher"/>
/// static HTTP client. Serves as the default implementation on platforms that
/// do not reference WebReaper; browser fetching is not supported and
/// <see cref="WebFetchOptions.UseBrowser"/> is ignored.
/// </summary>
public sealed class HttpClientWebFetcher : IWebFetcher
{
	/// <inheritdoc />
	public bool SupportsBrowser => false;

	/// <inheritdoc />
	public bool SupportsStealthBrowser => false;

	/// <inheritdoc />
	public Task<string> FetchContentAsync(string url, WebFetchOptions? options = null, CancellationToken cancellationToken = default)
		=> HtmlContentFetcher.FetchContentAsync(url, cancellationToken);

	/// <inheritdoc />
	public Task<FetchResult> FetchWithMetadataAsync(string url, WebFetchOptions? options = null, CancellationToken cancellationToken = default)
		=> HtmlContentFetcher.FetchWithMetadataAsync(url, cancellationToken);
}
