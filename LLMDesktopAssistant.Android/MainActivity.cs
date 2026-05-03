using Android.App;
using Android.Content.PM;
using Avalonia.Android;
using Avalonia.Controls.ApplicationLifetimes;
using LLMDesktopAssistant.Android.Utils;
using Serilog;
using System;

namespace LLMDesktopAssistant.Avalonia.Android
{
	[Activity(
		Label = "LLMDesktopAssistant.Avalonia.Android",
		Theme = "@style/MyTheme.NoActionBar",
		Icon = "@drawable/icon",
		MainLauncher = true,
		ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
	public class MainActivity : AvaloniaMainActivity
	{
		public MainActivity()
		{
			Log.Information("MainActivity created. Logging initialized.");
		}
	}
}
