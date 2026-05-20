using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using LiveMarkdown.Avalonia;
using LLMDesktopAssistant;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;
using Serilog;

namespace LLMDesktopAssistant
{
	public partial class App : Application
	{
		protected virtual LoggerConfiguration ConfigureLogger(LoggerConfiguration config)
		{
			return config
				.WriteTo.File(Path.Combine(Directories.LogFiles, "log.txt"), rollingInterval: RollingInterval.Day);
		}

		protected virtual void ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton<ILogger>(sp => Log.Logger);

			services.AddSingleton<SearXSharp.ILogger, SearXSharpLoggerAdapter>();
			services.AddSingleton<SearXSharp.SearchEngineManager>(sp =>
			{
				var logger = sp.GetRequiredService<SearXSharp.ILogger>();
				logger = SearXSharp.EmptyLogger.Instance; // Disable logging for now.
				var manager = new SearXSharp.SearchEngineManager(logger);
				
				//manager.RegisterEngines(SearXSharp.SearchEngines.AllEngines(logger));

				manager.RegisterEngines(
				[
					// Web
					SearXSharp.SearchEngines.Google(logger),
					SearXSharp.SearchEngines.Bing(logger),
					SearXSharp.SearchEngines.Brave(logger),
					SearXSharp.SearchEngines.DuckDuckGo(logger),
					SearXSharp.SearchEngines.Yandex(logger),
					SearXSharp.SearchEngines.Qwant(logger),
					SearXSharp.SearchEngines.Startpage(logger),
					SearXSharp.SearchEngines.Mojeek(logger),
					SearXSharp.SearchEngines.Searx(logger),
					SearXSharp.SearchEngines.Ask(logger),
					SearXSharp.SearchEngines.Yahoo(logger),

					// Images
					SearXSharp.SearchEngines.GoogleImages(logger),
					SearXSharp.SearchEngines.BingImages(logger),
					SearXSharp.SearchEngines.Pixabay(logger),
					SearXSharp.SearchEngines.Pexels(logger),
					SearXSharp.SearchEngines.Unsplash(logger),
					SearXSharp.SearchEngines.Flickr(logger),
					SearXSharp.SearchEngines.DeviantArt(logger),
					SearXSharp.SearchEngines.Pixiv(logger),
					SearXSharp.SearchEngines.Wallhaven(logger),
					SearXSharp.SearchEngines.WikiCommons(logger),
					SearXSharp.SearchEngines.Pinterest(logger),
					SearXSharp.SearchEngines.Imgur(logger),
					SearXSharp.SearchEngines.ArtStation(logger),

					// Videos
					SearXSharp.SearchEngines.YouTube(logger),
					SearXSharp.SearchEngines.Dailymotion(logger),
					SearXSharp.SearchEngines.Vimeo(logger),
					SearXSharp.SearchEngines.Bilibili(logger),
					SearXSharp.SearchEngines.GoogleVideos(logger),
					SearXSharp.SearchEngines.BingVideos(logger),
					SearXSharp.SearchEngines.Odysee(logger),
					SearXSharp.SearchEngines.PeerTube(logger),
					SearXSharp.SearchEngines.Rumble(logger),
					
					// News
					SearXSharp.SearchEngines.GoogleNews(logger),
					SearXSharp.SearchEngines.BingNews(logger),
					SearXSharp.SearchEngines.Reuters(logger),
					SearXSharp.SearchEngines.YahooNews(logger),

					// Maps
					SearXSharp.SearchEngines.OpenStreetMap(logger),
					SearXSharp.SearchEngines.Photon(logger),

					// Science
					SearXSharp.SearchEngines.GoogleScholar(logger),
					SearXSharp.SearchEngines.Wikipedia(logger),
					SearXSharp.SearchEngines.Arxiv(logger),
					SearXSharp.SearchEngines.Pubmed(logger),
					SearXSharp.SearchEngines.SemanticScholar(logger),
					SearXSharp.SearchEngines.OpenAlex(logger),
					SearXSharp.SearchEngines.WolframAlpha(logger),

					// Music
					SearXSharp.SearchEngines.Spotify(logger),
					SearXSharp.SearchEngines.SoundCloud(logger),
					SearXSharp.SearchEngines.Bandcamp(logger),
					SearXSharp.SearchEngines.Deezer(logger),
					SearXSharp.SearchEngines.Genius(logger),
					SearXSharp.SearchEngines.Mixcloud(logger),
					SearXSharp.SearchEngines.YandexMusic(logger),
					SearXSharp.SearchEngines.PodcastIndex(logger),
					SearXSharp.SearchEngines.Fyyd(logger),

					// Packages / Repos
					SearXSharp.SearchEngines.NuGet(logger),
					SearXSharp.SearchEngines.Npm(logger),
					SearXSharp.SearchEngines.Pypi(logger),
					SearXSharp.SearchEngines.Crates(logger),
					SearXSharp.SearchEngines.DockerHub(logger),
					SearXSharp.SearchEngines.Hex(logger),
					SearXSharp.SearchEngines.PkgGoDev(logger),
					SearXSharp.SearchEngines.MetaCPAN(logger),
					SearXSharp.SearchEngines.AlpineLinux(logger),
					SearXSharp.SearchEngines.FDroid(logger),

					// Social Media
					SearXSharp.SearchEngines.Reddit(logger),
					SearXSharp.SearchEngines.Mastodon(logger),
					SearXSharp.SearchEngines.Lemmy(logger),
					SearXSharp.SearchEngines.Discourse(logger),

					// Shopping / Entertainment
					SearXSharp.SearchEngines.Imdb(logger),
					SearXSharp.SearchEngines.Ebay(logger),

					// Books
					SearXSharp.SearchEngines.Goodreads(logger),
					SearXSharp.SearchEngines.OpenLibrary(logger),

					// Files / Torrents
					SearXSharp.SearchEngines.AnnasArchive(logger),
					SearXSharp.SearchEngines.ZLibrary(logger),
					SearXSharp.SearchEngines._1337x(logger),
					SearXSharp.SearchEngines.PirateBay(logger),
					SearXSharp.SearchEngines.Kickass(logger),
					SearXSharp.SearchEngines.Nyaa(logger),
				]);

				return manager;
			});
		}

		public override void Initialize()
		{
			Directories.EnsureAll();

			var loggerConfig = new LoggerConfiguration()
				.MinimumLevel.Debug();
			Log.Logger = ConfigureLogger(loggerConfig)
				.CreateLogger();

			// PluginManager.LoadPluginsInto(AppDomain.CurrentDomain); <- This method is called in desktop version
			ReflectionUtility.Initialize(AppDomain.CurrentDomain);
			ServiceRegistry.Initialize(
			[
				Log.Logger,
			], ConfigureServices);

			AvaloniaXamlLoader.Load(this);
		}

		public static MainView? MainView { get; private set; }
		public static MainWindow? MainWindow { get; private set; }

		private static TopLevel? _mainTopLevel;
		public static TopLevel MainTopLevel => _mainTopLevel ??= TopLevel.GetTopLevel((Control?)MainWindow ?? MainView)!;

		public override void OnFrameworkInitializationCompleted()
		{
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				DisableAvaloniaDataAnnotationValidation();

				MainWindow = new MainWindow
				{
					DataContext = new MainViewModel()
				};
				desktop.MainWindow = MainWindow;
			}
			else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
			{
				MainView = new MainView
				{
					DataContext = new MainViewModel()
				};
				singleViewPlatform.MainView = MainView;
			}
			else
			{
			}

			base.OnFrameworkInitializationCompleted();
		}

		private void DisableAvaloniaDataAnnotationValidation()
		{
			// TODO: Remove comments

			/*// Get an array of plugins to remove
			var dataValidationPluginsToRemove =
				BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

			// remove each entry found
			foreach (var plugin in dataValidationPluginsToRemove)
			{
				BindingPlugins.DataValidators.Remove(plugin);
			}*/
		}
	}
}