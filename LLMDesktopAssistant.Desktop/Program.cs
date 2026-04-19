using Avalonia;
using System;

namespace LLMDesktopAssistant.Desktop
{
	internal sealed class Program
	{
		[STAThread]
		public static void Main(string[] args) => BuildAvaloniaApp()
			.StartWithClassicDesktopLifetime(args);

		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<DesktopApp>()
				.UsePlatformDetect()
				.WithInterFont()
				.LogToTrace();
	}
}
