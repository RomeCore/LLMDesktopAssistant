using LLMDesktopAssistant.Core.Services;
using LLMDesktopAssistant.Core.Utils;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System.Windows;

namespace LLMDesktopAssistant.Core
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

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

			// AllocConsole();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			ServiceRegistry.Shutdown();

			base.OnExit(e);
		}
	}
}