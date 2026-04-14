using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using LLMDesktopAssistant.Avalonia.MVVM;
using LLMDesktopAssistant.Core;
using LLMDesktopAssistant.Core.Services;
using LLMDesktopAssistant.Core.Utils;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System.Linq;

namespace LLMDesktopAssistant.Avalonia
{
	public partial class App : Application
	{
		public override void Initialize()
		{
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

			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.WriteTo.Sink(new ConsoleAllocatorSink())
				.WriteTo.Console(applyThemeToRedirectedOutput: true, theme: SystemConsoleTheme.Literate)
				.WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
				.CreateLogger();

			Directories.EnsureAll();
			PluginManager.LoadPluginsInto(AppDomain.CurrentDomain);
			ReflectionUtility.Initialize(AppDomain.CurrentDomain);
			ServiceRegistry.Initialize([ Log.Logger, ]);

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