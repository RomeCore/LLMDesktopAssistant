using System;
using System.Collections.Generic;
using System.Text;
using LLMDesktopAssistant.Utils;
using SearXSharp;

namespace LLMDesktopAssistant.Services.Configurators
{
	[ServiceConfigurator]
	public class SearXSharpConfigurator : ServiceConfigurator
	{
		public override void Configure(IServiceCollection services)
		{
			services.AddSingleton<SearXSharp.ILogger, SearXSharpLoggerAdapter>();
			services.AddSingleton<SearchEngineManager>(sp =>
			{
				var logger = sp.GetRequiredService<SearXSharp.ILogger>();
				logger = SearXSharp.EmptyLogger.Instance; // Disable logging for now.
				var manager = new SearchEngineManager(logger);

				//manager.RegisterEngines(SearXSharp.SearchEngines.AllEngines(logger));

				manager.RegisterEngines(
				[
					// Web
					SearchEngines.Google(logger),
					SearchEngines.Bing(logger),
					SearchEngines.Brave(logger),
					SearchEngines.DuckDuckGo(logger),
					SearchEngines.Yandex(logger),
					SearchEngines.Qwant(logger),
					SearchEngines.Startpage(logger),
					SearchEngines.Mojeek(logger),
					SearchEngines.Searx(logger),
					SearchEngines.Ask(logger),
					SearchEngines.Yahoo(logger),

					// Images
					SearchEngines.GoogleImages(logger),
					SearchEngines.BingImages(logger),
					SearchEngines.Pixabay(logger),
					SearchEngines.Pexels(logger),
					SearchEngines.Unsplash(logger),
					SearchEngines.Flickr(logger),
					SearchEngines.DeviantArt(logger),
					SearchEngines.Pixiv(logger),
					SearchEngines.Wallhaven(logger),
					SearchEngines.WikiCommons(logger),
					SearchEngines.Pinterest(logger),
					SearchEngines.Imgur(logger),
					SearchEngines.ArtStation(logger),

					// Videos
					SearchEngines.YouTube(logger),
					SearchEngines.Dailymotion(logger),
					SearchEngines.Vimeo(logger),
					SearchEngines.Bilibili(logger),
					SearchEngines.GoogleVideos(logger),
					SearchEngines.BingVideos(logger),
					SearchEngines.Odysee(logger),
					SearchEngines.PeerTube(logger),
					SearchEngines.Rumble(logger),
					
					// News
					SearchEngines.GoogleNews(logger),
					SearchEngines.BingNews(logger),
					SearchEngines.Reuters(logger),
					SearchEngines.YahooNews(logger),

					// Maps
					SearchEngines.OpenStreetMap(logger),
					SearchEngines.Photon(logger),

					// Science
					SearchEngines.GoogleScholar(logger),
					SearchEngines.Wikipedia(logger),
					SearchEngines.Arxiv(logger),
					SearchEngines.Pubmed(logger),
					SearchEngines.SemanticScholar(logger),
					SearchEngines.OpenAlex(logger),
					SearchEngines.WolframAlpha(logger),

					// Music
					SearchEngines.Spotify(logger),
					SearchEngines.SoundCloud(logger),
					SearchEngines.Bandcamp(logger),
					SearchEngines.Deezer(logger),
					SearchEngines.Genius(logger),
					SearchEngines.Mixcloud(logger),
					SearchEngines.YandexMusic(logger),
					SearchEngines.PodcastIndex(logger),
					SearchEngines.Fyyd(logger),

					// Packages / Repos
					SearchEngines.NuGet(logger),
					SearchEngines.Npm(logger),
					SearchEngines.Pypi(logger),
					SearchEngines.Crates(logger),
					SearchEngines.DockerHub(logger),
					SearchEngines.Hex(logger),
					SearchEngines.PkgGoDev(logger),
					SearchEngines.MetaCPAN(logger),
					SearchEngines.AlpineLinux(logger),
					SearchEngines.FDroid(logger),

					// Social Media
					SearchEngines.Reddit(logger),
					SearchEngines.Mastodon(logger),
					SearchEngines.Lemmy(logger),
					SearchEngines.Discourse(logger),

					// Shopping / Entertainment
					SearchEngines.Imdb(logger),
					SearchEngines.Ebay(logger),

					// Books
					SearchEngines.Goodreads(logger),
					SearchEngines.OpenLibrary(logger),

					// Files / Torrents
					SearchEngines.AnnasArchive(logger),
					SearchEngines.ZLibrary(logger),
					SearchEngines._1337x(logger),
					SearchEngines.PirateBay(logger),
					SearchEngines.Kickass(logger),
					SearchEngines.Nyaa(logger),
				]);

				return manager;
			});
		}
	}
}