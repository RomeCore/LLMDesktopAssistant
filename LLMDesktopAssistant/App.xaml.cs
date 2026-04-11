using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows;
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.Utils;
using Python.Runtime;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace LLMDesktopAssistant
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App()
		{
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.WriteTo.Sink(new ConsoleAllocatorSink())
				.WriteTo.Console(applyThemeToRedirectedOutput: true, theme: SystemConsoleTheme.Literate)
				.WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
				.CreateLogger();
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			Directories.EnsureAll();
			PluginManager.LoadPluginsInto(AppDomain.CurrentDomain);
			ReflectionUtility.Initialize(AppDomain.CurrentDomain);
			ModuleManager.Initialize();

			// AllocConsole();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			ModuleManager.Shutdown();

			base.OnExit(e);
		}
	}
}