namespace LLMDesktopAssistant.Utils.Web;

/// <summary>
/// The result of a page fetch operation containing the raw HTML, HTTP status code, and response headers.
/// </summary>
public sealed record FetchResult(
	string Html,
	int? HttpStatus,
	IReadOnlyDictionary<string, string> Headers
);
