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
					// General
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