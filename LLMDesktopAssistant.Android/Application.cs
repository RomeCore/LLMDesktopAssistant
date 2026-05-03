using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace LLMDesktopAssistant.Avalonia.Android
{
	[Application]
	public class Application : AvaloniaAndroidApplication<AndroidApp>
	{
		protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
		}

		protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
		{
			return base.CustomizeAppBuilder(builder)
				.WithInterFont();
		}
	}
}
