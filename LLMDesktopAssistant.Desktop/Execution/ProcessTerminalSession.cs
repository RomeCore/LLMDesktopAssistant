using Porta.Pty;
using XTerm;

namespace LLMDesktopAssistant.Desktop.Execution
{
	public class ProcessTerminalSession
	{
		/// <summary>
		/// The PTY connection for the process.
		/// </summary>
		public required IPtyConnection Pty { get; init; }

		/// <summary>
		/// The terminal instance associated with the process.
		/// </summary>
		public required Terminal Terminal { get; init; }
	}
}
