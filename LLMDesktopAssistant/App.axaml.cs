using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using LiveMarkdown.Avalonia;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;
using Serilog;
using System.IO;
using System.Linq;
using System.Diagnostics;
using LLMDesktopAssistant.WebSearch;
using Serilog.Core;

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