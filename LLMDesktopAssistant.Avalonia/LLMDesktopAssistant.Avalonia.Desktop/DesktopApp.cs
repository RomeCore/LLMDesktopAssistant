using LLMDesktopAssistant.Avalonia.Desktop.Utils;
using LLMDesktopAssistant.Core;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System;

namespace LLMDesktopAssistant.Avalonia.Desktop
{
	public class DesktopApp : App
	{
		protected override LoggerConfiguration ConfigureLogger(LoggerConfiguration config)
		{
			return base.ConfigureLogger(config)
				.WriteTo.Sink(new ConsoleAllocatorSink())
				.WriteTo.Console(applyThemeToRedirectedOutput: true, theme: SystemConsoleTheme.Literate);
		}

		public override void Initialize()
		{
			// Load plugins only for desktops
			PluginManager.LoadPluginsInto(AppDomain.CurrentDomain);

			base.Initialize();
		}
	}
}
