using Material.Icons;
using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.Desktop.ToolModules.Terminal
{
	/// <summary>
	/// Parameters for running a process in a terminal.
	/// </summary>
	public class TerminalToolRunParameters
	{
		/// <summary>
		/// The status icon to set into <see cref="ReactiveToolResult"/>.
		/// </summary>
		public MaterialIconKind? StatusIcon { get; init; }

		/// <summary>
		/// The status title to set into <see cref="ReactiveToolResult"/>.
		/// </summary>
		public string? StatusTitle { get; init; }

		/// <summary>
		/// Whether to run the terminal with interactive UI or just run an external process.
		/// If true, a full interactive shell is launched. If false, just runs the command.
		/// </summary>
		public bool RunTerminal { get; init; }

		/// <summary>
		/// The name of the process to run in the terminal.
		/// If not provided and <see cref="Command"/> is set, uses system shell (cmd/bash).
		/// If neither is set, opens default shell.
		/// </summary>
		public string? ProcessName { get; init; }

		/// <summary>
		/// Arguments for the process. Used only when <see cref="ProcessName"/> is set.
		/// </summary>
		public string[]? Arguments { get; init; }

		/// <summary>
		/// The command to run in the terminal via system shell.
		/// On Windows runs via cmd.exe /c, on Linux via bash -c.
		/// Ignored if <see cref="ProcessName"/> is set.
		/// </summary>
		public string? Command { get; init; }

		/// <summary>
		/// The working directory to run the process in.
		/// </summary>
		public string? WorkingDirectory { get; init; }
	}
}
