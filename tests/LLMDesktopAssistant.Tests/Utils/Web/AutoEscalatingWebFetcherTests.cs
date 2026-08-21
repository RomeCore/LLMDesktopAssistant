using LLMDesktopAssistant.Utils.Web;
using Microsoft.Extensions.Logging.Abstractions;

namespace LLMDesktopAssistant.Tests.Utils.Web;

/// <summary>
/// Tests for the <see cref="AutoEscalatingWebFetcher"/> decorator.
/// </summary>
public class AutoEscalatingWebFetcherTests
{
	private sealed class FakeWebFetcher : IWebFetcher
	{
		private readonly Func<WebFetchLevel, FetchResult> _handler;

		public FakeWebFetcher(WebFetchLevel maxLevel, Func<WebFetchLevel, FetchResult> handler)
		{
			MaxLevel = maxLevel;
			_handler = handler;
		}

		public WebFetchLevel MaxLevel { get; }

		public List<WebFetchLevel> RequestedLevels { get; } = [];

		public Task<FetchResult> FetchAsync(string url, WebFetchLevel minFetchLevel = WebFetchLevel.HttpClient, CancellationToken cancellationToken = default)
		{
			RequestedLevels.Add(minFetchLevel);
			return Task.FromResult(_handler(minFetchLevel));
		}
	}

	private static FetchResult Result(int status) => new("body", status, new Dictionary<string, string>());

	private static AutoEscalatingWebFetcher Wrap(FakeWebFetcher inner)
		=> new(inner, NullLogger.Instance);

	[Fact]
	public async Task FetchAsync_DoesNotEscalate_OnSuccess()
	{
		var inner = new FakeWebFetcher(WebFetchLevel.StealthBrowser, _ => Result(200));
		var fetcher = Wrap(inner);

		var result = await fetcher.FetchAsync("https://example.com");

		Assert.Equal(200, result.HttpStatus);
		Assert.Equal([WebFetchLevel.HttpClient], inner.RequestedLevels);
	}

	[Fact]
	public async Task FetchAsync_DoesNotEscalate_OnNotFound()
	{
		var inner = new FakeWebFetcher(WebFetchLevel.StealthBrowser, _ => Result(404));
		var fetcher = Wrap(inner);

		var result = await fetcher.FetchAsync("https://example.com");

		Assert.Equal(404, result.HttpStatus);
		Assert.Equal([WebFetchLevel.HttpClient], inner.RequestedLevels);
	}

	[Theory]
	[InlineData(401)]
	[InlineData(403)]
	[InlineData(407)]
	[InlineData(429)]
	[InlineData(451)]
	public async Task FetchAsync_EscalatesOnAccessDenied_UntilSuccess(int deniedStatus)
	{
		var inner = new FakeWebFetcher(WebFetchLevel.StealthBrowser, level =>
			level == WebFetchLevel.HttpClient ? Result(deniedStatus) : Result(200));
		var fetcher = Wrap(inner);

		var result = await fetcher.FetchAsync("https://example.com");

		Assert.Equal(200, result.HttpStatus);
		Assert.Equal([WebFetchLevel.HttpClient, WebFetchLevel.Browser], inner.RequestedLevels);
	}

	[Fact]
	public async Task FetchAsync_EscalatesThroughAllLevels_WhenAlwaysDenied()
	{
		var inner = new FakeWebFetcher(WebFetchLevel.StealthBrowser, _ => Result(403));
		var fetcher = Wrap(inner);

		var result = await fetcher.FetchAsync("https://example.com");

		Assert.Equal(403, result.HttpStatus);
		Assert.Equal(
			[WebFetchLevel.HttpClient, WebFetchLevel.Browser, WebFetchLevel.StealthBrowser],
			inner.RequestedLevels);
	}

	[Fact]
	public async Task FetchAsync_StartsAtRequestedLevel()
	{
		var inner = new FakeWebFetcher(WebFetchLevel.StealthBrowser, _ => Result(200));
		var fetcher = Wrap(inner);

		var result = await fetcher.FetchAsync("https://example.com", WebFetchLevel.Browser);

		Assert.Equal(200, result.HttpStatus);
		Assert.Equal([WebFetchLevel.Browser], inner.RequestedLevels);
	}

	[Fact]
	public async Task FetchAsync_ClampsRequestedLevel_ToMaxLevel()
	{
		var inner = new FakeWebFetcher(WebFetchLevel.Browser, _ => Result(200));
		var fetcher = Wrap(inner);

		var result = await fetcher.FetchAsync("https://example.com", WebFetchLevel.StealthBrowser);

		Assert.Equal(200, result.HttpStatus);
		Assert.Equal([WebFetchLevel.Browser], inner.RequestedLevels);
	}

	[Fact]
	public async Task FetchAsync_ForwardsMaxLevel()
	{
		var inner = new FakeWebFetcher(WebFetchLevel.StealthBrowser, _ => Result(200));
		var fetcher = Wrap(inner);

		Assert.Equal(WebFetchLevel.StealthBrowser, fetcher.MaxLevel);
	}

	[Fact]
	public async Task FetchAsync_EscalationCappedBySettings_WhenStealthDisabled()
	{
		await AppSettingsLock.LockAsync(async (settings, ct) =>
		{
			settings.WebFetch.EnableStealthBrowser = false;

			var inner = new FakeWebFetcher(WebFetchLevel.StealthBrowser, _ => Result(403));
			var fetcher = Wrap(inner);

			var result = await fetcher.FetchAsync("https://example.com");

			Assert.Equal(403, result.HttpStatus);
			Assert.Equal([WebFetchLevel.HttpClient, WebFetchLevel.Browser], inner.RequestedLevels);
		});
	}

	[Fact]
	public async Task FetchAsync_DoesNotEscalate_WhenBrowsersDisabled()
	{
		await AppSettingsLock.LockAsync(async (settings, ct) =>
		{
			settings.WebFetch.EnableBrowser = false;
			settings.WebFetch.EnableStealthBrowser = false;

			var inner = new FakeWebFetcher(WebFetchLevel.StealthBrowser, _ => Result(403));
			var fetcher = Wrap(inner);

			var result = await fetcher.FetchAsync("https://example.com");

			Assert.Equal(403, result.HttpStatus);
			Assert.Equal([WebFetchLevel.HttpClient], inner.RequestedLevels);
		});
	}

	[Fact]
	public async Task FetchAsync_RaisesToDefaultLevel()
	{
		await AppSettingsLock.LockAsync(async (settings, ct) =>
		{
			settings.WebFetch.DefaultFetchLevel = WebFetchLevel.Browser;

			var inner = new FakeWebFetcher(WebFetchLevel.StealthBrowser, _ => Result(200));
			var fetcher = Wrap(inner);

			var result = await fetcher.FetchAsync("https://example.com");

			Assert.Equal(200, result.HttpStatus);
			Assert.Equal([WebFetchLevel.Browser], inner.RequestedLevels);
		});
	}

	[Fact]
	public async Task FetchAsync_DefaultLevelClampedBySettings()
	{
		await AppSettingsLock.LockAsync(async (settings, ct) =>
		{
			settings.WebFetch.DefaultFetchLevel = WebFetchLevel.StealthBrowser;
			settings.WebFetch.EnableStealthBrowser = false;

			var inner = new FakeWebFetcher(WebFetchLevel.StealthBrowser, _ => Result(200));
			var fetcher = Wrap(inner);

			var result = await fetcher.FetchAsync("https://example.com");

			Assert.Equal(200, result.HttpStatus);
			Assert.Equal([WebFetchLevel.Browser], inner.RequestedLevels);
		});
	}

	[Fact]
	public async Task FetchAsync_RequestedLevelClampedBySettings()
	{
		await AppSettingsLock.LockAsync(async (settings, ct) =>
		{
			settings.WebFetch.EnableBrowser = false;
			settings.WebFetch.EnableStealthBrowser = false;

			var inner = new FakeWebFetcher(WebFetchLevel.StealthBrowser, _ => Result(200));
			var fetcher = Wrap(inner);

			var result = await fetcher.FetchAsync("https://example.com", WebFetchLevel.StealthBrowser);

			Assert.Equal(200, result.HttpStatus);
			Assert.Equal([WebFetchLevel.HttpClient], inner.RequestedLevels);
		});
	}

	[Theory]
	[InlineData(401, true)]
	[InlineData(403, true)]
	[InlineData(407, true)]
	[InlineData(429, true)]
	[InlineData(451, true)]
	[InlineData(400, false)]
	[InlineData(404, false)]
	[InlineData(500, false)]
	[InlineData(200, false)]
	[InlineData(null, false)]
	public void IsAccessDenied_ClassifiesStatuses(int? status, bool expected)
		=> Assert.Equal(expected, AutoEscalatingWebFetcher.IsAccessDenied(status));
}
