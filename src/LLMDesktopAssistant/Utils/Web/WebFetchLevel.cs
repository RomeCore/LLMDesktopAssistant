namespace LLMDesktopAssistant.Utils.Web;

/// <summary>
/// The level of effort used to fetch a web page: from a plain HTTP request
/// to a stealth browser that evades advanced anti-bot systems.
/// </summary>
public enum WebFetchLevel
{
	/// <summary>
	/// A plain HTTP request. Fastest, but does not render JavaScript and is
	/// easily blocked by anti-bot systems.
	/// </summary>
	HttpClient = 0,

	/// <summary>
	/// A headless browser that renders JavaScript and bypasses basic anti-bot
	/// checks.
	/// </summary>
	Browser = 1,

	/// <summary>
	/// The stealth CloakBrowser that evades advanced anti-bot systems
	/// (Cloudflare, reCAPTCHA v3, FingerprintJS). Slowest option.
	/// </summary>
	StealthBrowser = 2,
}
