using LLMDesktopAssistant.Core.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Core.Scripting
{
	/// <summary>
	/// Provides methods for executing shell commands on Windows and WSL (Bash).
	/// </summary>
	public static class ShellExecutor
	{
		/// <summary>
		/// Executes a Windows shell command asynchronously.
		/// </summary>
		/// <param name="command">The command to execute.</param>
		/// <param name="workDir">The working directory for the command. If null, the current directory is used.</param>
		/// <returns>The result of the command execution.</returns>
		public static async Task<ProcessExecutionResult> ExecuteWindowsAsync(string command,
			string? workDir = null, CancellationToken cancellationToken = default)
		{
			return await ExecuteProcessAsync("cmd.exe", $"/c \"{command}\"",
				workDir, cancellationToken);
		}

		/// <summary>
		/// Executes a multiline Windows shell script asynchronously.
		/// </summary>
		/// <param name="script">The command to execute.</param>
		/// <param name="workDir">The working directory for the command. If null, the current directory is used.</param>
		/// <returns>The result of the command execution.</returns>
		public static async Task<ProcessExecutionResult> ExecuteWindowsScriptAsync(string script,
			string? workDir = null, CancellationToken cancellationToken = default)
		{
			var tempFile = Path.Combine(Environment.CurrentDirectory, Directories.TempScripts, $"{Guid.NewGuid()}.bat");
			File.WriteAllText(tempFile, script);

			try
			{
				return await ExecuteProcessAsync("cmd.exe", $"/c \"{tempFile}\"", workDir, cancellationToken);
			}
			finally
			{
				if (File.Exists(tempFile))
					File.Delete(tempFile);
			}
		}

		/// <summary>
		/// Executes a Windows Powershell command asynchronously.
		/// </summary>
		/// <param name="command">The command to execute.</param>
		/// <param name="workDir">The working directory for the command. If null, the current directory is used.</param>
		/// <returns>The result of the command execution.</returns>
		public static async Task<ProcessExecutionResult> ExecuteWindowsPSAsync(string command,
			string? workDir = null, CancellationToken cancellationToken = default)
		{
			return await ExecuteProcessAsync("powershell.exe", $"-ExecutionPolicy Bypass -Command \"{command}\"",
				workDir, cancellationToken);
		}

		/// <summary>
		/// Executes a multiline Windows Powershell script asynchronously.
		/// </summary>
		/// <param name="script">The command to execute.</param>
		/// <param name="workDir">The working directory for the command. If null, the current directory is used.</param>
		/// <returns>The result of the command execution.</returns>
		public static async Task<ProcessExecutionResult> ExecuteWindowsPSScriptAsync(string script,
			string? workDir = null, CancellationToken cancellationToken = default)
		{
			var tempFile = Path.Combine(Environment.CurrentDirectory, Directories.TempScripts, $"{Guid.NewGuid()}.ps1");
			File.WriteAllText(tempFile, script);

			try
			{
				return await ExecuteProcessAsync("powershell.exe", $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{tempFile}\"",
					workDir, cancellationToken);
			}
			finally
			{
				if (File.Exists(tempFile))
					File.Delete(tempFile);
			}
		}

		private static string ConvertToWslPath(string windowsPath)
		{
			if (string.IsNullOrWhiteSpace(windowsPath))
				return windowsPath;
			windowsPath = windowsPath.Replace('\\', '/');

			// Handle drive letter (C:/path -> /mnt/host/c/path)
			if (windowsPath.Length >= 2 && windowsPath[1] == ':')
			{
				char drive = char.ToLower(windowsPath[0]);
				string pathAfterDrive = windowsPath.Substring(2);
				return $"/mnt/host/{drive}{pathAfterDrive}";
			}

			return windowsPath;
		}

		/// <summary>
		/// Executes a Bash command via WSL asynchronously.
		/// </summary>
		/// <param name="command">The Bash command to execute.</param>
		/// <param name="workDir">The working directory (Windows path will be converted to WSL path).</param>
		/// <returns>The result of the command execution.</returns>
		public static async Task<ProcessExecutionResult> ExecuteBashAsync(string command,
			string? workDir = null, CancellationToken cancellationToken = default)
		{
			return await ExecuteProcessAsync("wsl", $"-- sh -c \"{command}\"", workDir, cancellationToken);
		}

		/// <summary>
		/// Executes a multiline Bash script via WSL asynchronously.
		/// </summary>
		/// <param name="script">The Bash script content.</param>
		/// <param name="workDir">The working directory (Windows path will be converted to WSL path).</param>
		/// <returns>The result of the command execution.</returns>
		public static async Task<ProcessExecutionResult> ExecuteBashScriptAsync(string script,
			string? workDir = null, CancellationToken cancellationToken = default)
		{
			string tempScript = Path.Combine(Environment.CurrentDirectory, Directories.TempScripts, $"{Guid.NewGuid()}.sh");

			// Add shebang and make script executable-friendly
			script = script.Replace("\r\n", "\n"); // Ensure Unix-style line endings
			File.WriteAllText(tempScript, script, Encoding.UTF8);

			try
			{
				// Convert Windows path to WSL path for the script
				string wslScriptPath = ConvertToWslPath(tempScript);

				// Make script executable (WSL can execute it directly)
				await ExecuteProcessAsync("wsl", $"chmod +x \"{wslScriptPath}\"", workDir, cancellationToken);
				return await ExecuteProcessAsync("wsl", $"\"{wslScriptPath}\"", workDir, cancellationToken);
			}
			finally
			{
				if (File.Exists(tempScript))
					File.Delete(tempScript);
			}
		}

		/// <summary>
		/// Core method for process execution.
		/// </summary>
		private static async Task<ProcessExecutionResult> ExecuteProcessAsync(string fileName, string arguments,
			string? workDir, CancellationToken cancellationToken = default)
		{
			var psi = new ProcessStartInfo
			{
				FileName = fileName,
				Arguments = arguments,
				WorkingDirectory = workDir ?? Environment.CurrentDirectory,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var process = new Process { StartInfo = psi };

			try
			{
				process.Start();

				var stdoutTask = process.StandardOutput.ReadToEndAsync();
				var stderrTask = process.StandardError.ReadToEndAsync();

				await Task.WhenAll(stdoutTask, stderrTask);

				try
				{
					await process.WaitForExitAsync(cancellationToken);
				}
				catch (AggregateException aex) when (aex.InnerExceptions.Any(e => e is OperationCanceledException))
				{
					process.Kill();
				}
				catch (OperationCanceledException)
				{
					process.Kill();
				}

				return new ProcessExecutionResult
				{
					StdOut = stdoutTask.Result,
					StdErr = stderrTask.Result,
					ExitCode = process.ExitCode
				};
			}
			catch (Exception ex)
			{
				return new ProcessExecutionResult
				{
					StdOut = string.Empty,
					StdErr = $"Failed to execute process: {ex.Message}",
					ExitCode = -1
				};
			}
		}
	}
}