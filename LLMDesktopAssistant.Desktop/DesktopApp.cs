using LLMDesktopAssistant.Desktop.Utils;
using LLMDesktopAssistant;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Blazor;

namespace LLMDesktopAssistant.Desktop
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
			ReflectionUtility.AddAdditionalAssembly(typeof(ChatBlazorUIStarter).Assembly, observe: true);

			base.Initialize();
		}
	}
}
