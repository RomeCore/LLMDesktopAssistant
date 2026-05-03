using Android.Util;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Android.Utils
{
	public class AndroidDebugLogSink : ILogEventSink
	{
		public void Emit(LogEvent logEvent)
		{
			var tag = "dASS";
			var message = logEvent.RenderMessage();

			switch (logEvent.Level)
			{
				case LogEventLevel.Verbose:
					Log.Verbose(tag, message);
					break;

				case LogEventLevel.Debug:
					Log.Debug(tag, message);
					break;

				case LogEventLevel.Information:
					Log.Info(tag, message);
					break;

				case LogEventLevel.Warning:
					Log.Warn(tag, message);
					break;

				case LogEventLevel.Error:
					Log.Error(tag, message);
					break;

				case LogEventLevel.Fatal:
					Log.Error(tag, message);
					break;

				default:
					Log.Info(tag, message);
					break;
			}
		}
	}
}