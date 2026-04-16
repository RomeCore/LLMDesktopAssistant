using LLMDesktopAssistant.Core.Utils;
using Serilog.Core;
using Serilog.Events;

namespace LLMDesktopAssistant.Avalonia.Desktop.Utils
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