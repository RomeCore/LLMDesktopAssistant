using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace LLMDesktopAssistant
{
	public partial class App : Application
	{
		/// <summary>
		/// Gets the current version of the application.
		/// </summary>
		public static Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

		protected virtual LoggerConfiguration ConfigureLogger(LoggerConfiguration config)
		{
			return config
				.WriteTo.File(Path.Combine(Directories.LogFiles, "log.txt"), rollingInterval: RollingInterval.Day);
		}

		protected virtual void ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton(sp => Log.Logger);
			services.AddLogging(l => l.AddProvider(new SerilogLoggerProvider(Log.Logger)));
			services.AddTransient(sp =>
			{
				var factory = sp.GetRequiredService<ILoggerFactory>();
				return factory.CreateLogger("Uncategorized");
			});
		}

		public override void Initialize()
		{
			if (Design.IsDesignMode)
			{
				AvaloniaXamlLoader.Load(this);
				return;
			}

			Directories.ClearTempFiles();
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
	}
}