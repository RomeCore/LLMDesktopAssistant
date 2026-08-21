namespace LLMDesktopAssistant.Utils.Web;

/// <summary>
/// An <see cref="IWebFetcher"/> backed by the <see cref="HtmlContentFetcher"/>
/// static HTTP client. Serves as the default implementation on platforms that
/// do not reference WebReaper; browser fetching is not supported.
/// </summary>
public sealed class HttpClientWebFetcher : IWebFetcher
{
	/// <inheritdoc />
	public WebFetchLevel MaxLevel => WebFetchLevel.HttpClient;

	/// <inheritdoc />
	public Task<FetchResult> FetchAsync(string url, WebFetchLevel minFetchLevel = WebFetchLevel.HttpClient, CancellationToken cancellationToken = default)
		=> HtmlContentFetcher.FetchAsync(url, cancellationToken);
}
