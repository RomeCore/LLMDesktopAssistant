using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Tools;
using RCLargeLanguageModels.Tools;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Desktop.ToolModules.Terminal
{
	/// <summary>
	/// Base class for tool modules that run processes and display their output
	/// in a terminal emulator embedded in the chat message.
	/// </summary>
	public abstract class TerminalBasedToolModule : ToolModule
	{
		/// <summary>
		/// Runs a process with terminal output displayed in the chat message.
		/// Creates a <see cref="TerminalAdditionalViewModel"/>, adds it to the message's
		/// AdditionalViewModels collection, and waits for the process to complete.
		/// </summary>
		/// <param name="parameters">Parameters describing what to run.</param>
		/// <param name="context">The tool execution context (provides access to the chat message).</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>A ReactiveToolResult with the process exit code.</returns>
		protected Task<ReactiveToolResult> RunAsync(
			TerminalRunParameters parameters,
			ToolExecutionContext context,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(parameters);
			ArgumentNullException.ThrowIfNull(context);

			if (parameters.RunTerminal)
				return RunInTerminalAsync(parameters, context, cancellationToken);
			else
				return RunNonTerminalAsync(parameters, context, cancellationToken);
		}

		private async Task<ReactiveToolResult> RunNonTerminalAsync(
			TerminalRunParameters parameters,
			ToolExecutionContext context,
			CancellationToken cancellationToken = default)
		{
			var message = context.Message;
			string process;
			string[] args;
			string? workDir = parameters.WorkingDirectory;

			if (!string.IsNullOrEmpty(parameters.ProcessName))
			{
				// Explicit process specified
				process = parameters.ProcessName;
				args = parameters.Arguments ?? [];
			}
			else if (!string.IsNullOrEmpty(parameters.Command))
			{
				// Run command via system shell
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					process = "cmd.exe";
					args = ["/c " + parameters.Command];
				}
				else
				{
					process = "/bin/bash";
					args = ["-c " + parameters.Command];
				}
			}
			else
			{
				// Default: open interactive shell
				process = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "bash";
				args = [];
			}

			var result = await ShellExecutor.ExecuteProcessAsync(process, string.Join(" ", args), workDir, cancellationToken);

			var resultBuilder = new StringBuilder();
			resultBuilder.Append(result.StdOut);
			if (!string.IsNullOrEmpty(result.StdErr))
			{
				resultBuilder.AppendLine().AppendLine("STDERR:");
				resultBuilder.Append(result.StdErr);
			}

			return ReactiveToolResult.Create(result.Success, resultBuilder.ToString());
		}

		private async Task<ReactiveToolResult> RunInTerminalAsync(
			TerminalRunParameters parameters,
			ToolExecutionContext context,
			CancellationToken cancellationToken = default)
		{
			var message = context.Message;

			// Create the terminal view model
			var viewModel = new TerminalAdditionalViewModel
			{
				ProcessName = parameters.ProcessName ?? string.Empty,
				Arguments = parameters.Arguments ?? [],
				Command = parameters.Command ?? string.Empty,
				WorkingDirectory = parameters.WorkingDirectory,
			};

			// Add it to the message so it renders in the UI
			message.AdditionalViewModels.Add(viewModel);

			int exitCode;
			try
			{
				// Wait for the process to complete
				exitCode = await viewModel.ExitCodeTask.WaitAsync(cancellationToken);
			}
			catch (OperationCanceledException)
			{
				// User cancelled or token was cancelled
				viewModel.Cancel();
				return ReactiveToolResult.CreateError(viewModel.Output ?? string.Empty);
			}

			// Return result based on exit code
			if (exitCode == 0)
			{
				return ReactiveToolResult.CreateSuccess(viewModel.Output ?? string.Empty);
			}
			else
			{
				return ReactiveToolResult.CreateError(viewModel.Output +
					$"\nProcess exited with code {exitCode}. Check terminal output above for details.");
			}
		}
	}
}
