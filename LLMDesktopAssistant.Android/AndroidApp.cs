using LLMDesktopAssistant.Android.Utils;
using Serilog;

namespace LLMDesktopAssistant.Avalonia.Android
{
	public class AndroidApp : App
	{
		protected override LoggerConfiguration ConfigureLogger(LoggerConfiguration config)
		{
			return base.ConfigureLogger(config)
				.WriteTo.Sink(new AndroidDebugLogSink());
		}
	}
}
