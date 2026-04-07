using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows;
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.Utils;
using Python.Runtime;
using Serilog;

namespace LLMDesktopAssistant
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		private static extern void AllocConsole();

		public App()
		{
			AllocConsole();
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.WriteTo.Console()
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
		}

		protected override void OnExit(ExitEventArgs e)
		{
			ModuleManager.Shutdown();

			base.OnExit(e);
		}
	}
}