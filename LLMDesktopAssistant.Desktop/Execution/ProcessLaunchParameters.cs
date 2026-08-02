using System.Collections.Immutable;

namespace LLMDesktopAssistant.Desktop.Execution
{
	public class ProcessLaunchParameters
	{
		/// <summary>
		/// The name of the file to execute.
		/// </summary>
		public required string FileName { get; init; }

		/// <summary>
		/// A list of arguments to pass to the file.
		/// </summary>
		public required ImmutableList<string> Arguments { get; init; }

		/// <summary>
		/// The working directory for the process.
		/// </summary>
		public required string WorkingDirectory { get; init; }

		/// <summary>
		/// Whether to run process in the interactive terminal.
		/// </summary>
		public bool RunInTerminal { get; init; } = false;

		/// <summary>
		/// The environment variables to set for the process.
		/// </summary>
		public ImmutableDictionary<string, string> EnvironmentVariables { get; init; } = [];

		/// <summary>
		/// The standard input to pass to the process.
		/// </summary>
		public string? StdIn { get; init; }
	}
}
