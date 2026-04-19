namespace LLMDesktopAssistant.Scripting
{
	/// <summary>
	/// Represents the result of a Windows shell command execution.
	/// </summary>
	public class ProcessExecutionResult
	{
		/// <summary>
		/// Gets the standard output of the command.
		/// </summary>
		public required string StdOut { get; init; }

		/// <summary>
		/// Gets the standard error of the command.
		/// </summary>
		public required string StdErr { get; init; }

		/// <summary>
		/// Gets the exit code of the command.
		/// </summary>
		public required int ExitCode { get; init; }

		/// <summary>
		/// Gets a value indicating whether the command was successful. A successful command has an exit code of 0.
		/// </summary>
		public bool Success => ExitCode == 0;
	}
}