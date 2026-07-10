using System;
using Avalonia.Markup.Xaml.Styling;
using LLMDesktopAssistant;
using LLMDesktopAssistant.Blazor;
using LLMDesktopAssistant.Desktop.Utils;
using LLMDesktopAssistant.Utils;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

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

			Styles.Add(new StyleInclude(new Uri("avares://LiveMarkdown.Avalonia.Mermaid/Styles.axaml"))
			{
				Source = new Uri("avares://LiveMarkdown.Avalonia.Mermaid/Styles.axaml")
			});

			base.Initialize();
		}
	}
}
