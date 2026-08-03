using Porta.Pty;
using XTerm;

namespace LLMDesktopAssistant.Desktop.Execution
{
	public class ProcessTerminalSession : NotifyPropertyChanged
	{
		/// <summary>
		/// The PTY connection for the process.
		/// </summary>
		public required IPtyConnection Pty { get; init; }

		/// <summary>
		/// The terminal instance associated with the process.
		/// </summary>
		public required Terminal Terminal { get; init; }

		private string? _output;
		/// <summary>
		/// Standard output and error output of the process combined.
		/// </summary>
		public string Output => _output ??= Terminal.GetBufferContents();

		/// <summary>
		/// Raised when new output has been written to the <see cref="Terminal"/>.
		/// Fires on the background thread that pumps the PTY output.
		/// </summary>
		public event EventHandler? OutputUpdated;

		/// <summary>
		/// Raises the <see cref="OutputUpdated"/> event.
		/// </summary>
		internal void RaiseOutputUpdated()
		{
			_output = null;
			OutputUpdated?.Invoke(this, EventArgs.Empty);
		}
	}
}
