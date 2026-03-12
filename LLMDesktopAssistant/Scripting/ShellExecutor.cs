using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Scripting
{
	/// <summary>
	/// Provides methods for executing Windows shell commands.
	/// </summary>
	public static class ShellExecutor
	{
		/// <summary>
		/// Executes a Windows shell command asynchronously.
		/// </summary>
		/// <param name="command">The command to execute.</param>
		/// <param name="workDir">The working directory for the command. If null, the current directory is used.</param>
		/// <returns>The result of the command execution.</returns>
		public static async Task<ShellExecutionResult> ExecuteAsync(string command, string? workDir = null)
		{
			var psi = new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = $"/c \"{command}\"",
				WorkingDirectory = workDir ?? Environment.CurrentDirectory,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var process = new Process { StartInfo = psi };

			process.Start();

			var stdoutTask = process.StandardOutput.ReadToEndAsync();
			var stderrTask = process.StandardError.ReadToEndAsync();

			await Task.WhenAll(stdoutTask, stderrTask);
			await process.WaitForExitAsync();

			return new ShellExecutionResult
			{
				StdOut = stdoutTask.Result,
				StdErr = stderrTask.Result,
				ExitCode = process.ExitCode
			};
		}
	}
}