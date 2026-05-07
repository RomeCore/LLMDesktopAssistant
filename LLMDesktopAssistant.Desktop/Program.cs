using Avalonia;
using LLMDesktopAssistant.Desktop.Utils;
using Serilog;
using System;

namespace LLMDesktopAssistant.Desktop
{
	internal sealed class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			try
			{
				ConsoleManager.EnsureConsole();

				BuildAvaloniaApp()
					.StartWithClassicDesktopLifetime(args);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "A fatal error occurred: {Message}", ex.Message);
				throw;
			}
		}

		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<DesktopApp>()
				.UsePlatformDetect()
				.WithInterFont()
				.LogToTrace();
	}
}
