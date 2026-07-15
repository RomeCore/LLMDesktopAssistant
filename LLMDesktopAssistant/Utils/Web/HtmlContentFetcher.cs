using System;
using System.Collections.Generic;
using System.Text;
using AngleSharp;

namespace LLMDesktopAssistant.Utils.Web
{
	public static class HtmlContentFetcher
	{
		private static readonly AsyncCache<string, string> _cache =
			new(FetchContentFactory,slidingExpirationTime: TimeSpan.FromMinutes(15));

		private static async Task<string> FetchContentFactory(string url, CancellationToken cancellationToken = default)
		{
			var config = Configuration.Default.WithDefaultLoader();
			var context = BrowsingContext.New(config);
			var document = await context.OpenAsync(url);

			return document.Body?.OuterHtml ?? throw new HttpRequestException("Failed to fetch HTML content.");
		}

		public static async Task<string> FetchContent(string url, CancellationToken cancellationToken = default)
		{
			return await _cache.GetAsync(url, cancellationToken);
		}
	}
}
