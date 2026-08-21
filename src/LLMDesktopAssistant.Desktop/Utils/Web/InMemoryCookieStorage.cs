using System.Net;
using WebReaper.Core.CookieStorage.Abstract;

namespace LLMDesktopAssistant.Desktop.Utils.Web;

/// <summary>
/// An in-memory <see cref="ICookiesStorage"/> that keeps a single
/// <see cref="CookieContainer"/> per fetcher instance.
/// </summary>
internal sealed class InMemoryCookieStorage : ICookiesStorage
{
	private CookieContainer _cookies = new();

	public Task AddAsync(CookieContainer cookieCollection)
	{
		_cookies = cookieCollection;
		return Task.CompletedTask;
	}

	public Task<CookieContainer> GetAsync() => Task.FromResult(_cookies);
}
