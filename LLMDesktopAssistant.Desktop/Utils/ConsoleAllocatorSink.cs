using LLMDesktopAssistant.Utils;
using Serilog.Core;
using Serilog.Events;

namespace LLMDesktopAssistant.Desktop.Utils
{
	/// <summary>
	/// A Serilog sink that ensures a console is allocated before logging.
	/// </summary>
	public class ConsoleAllocatorSink : ILogEventSink
	{
		public void Emit(LogEvent logEvent)
		{
			ConsoleManager.EnsureConsole();
		}
	}
}