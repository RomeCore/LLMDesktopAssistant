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
			TerminalToolRunParameters parameters,
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
			TerminalToolRunParameters parameters,
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

			var result = new ReactiveToolResult
			{
				StatusIcon = parameters.StatusIcon,
				StatusTitle = parameters.StatusTitle
			};

			_ = Task.Run(async () =>
			{
				var executionResult = await ShellExecutor.ExecuteProcessAsync(process, string.Join(" ", args), workDir, cancellationToken);

				var resultBuilder = new StringBuilder();
				resultBuilder.Append(executionResult.StdOut);
				if (!string.IsNullOrEmpty(executionResult.StdErr))
				{
					resultBuilder.AppendLine().AppendLine("STDERR:");
					resultBuilder.Append(executionResult.StdErr);
				}

				result.ResultContent = resultBuilder.ToString();
				result.CompleteWithSuccess();
			}, cancellationToken);

			return result;
		}

		private async Task<ReactiveToolResult> RunInTerminalAsync(
			TerminalToolRunParameters parameters,
			ToolExecutionContext context,
			CancellationToken cancellationToken = default)
		{
			var message = context.Message;

			var result = new ReactiveToolResult
			{
				StatusIcon = parameters.StatusIcon,
				StatusTitle = parameters.StatusTitle
			};

			_ = Task.Run(async () =>
			{
				var viewModel = new TerminalAdditionalViewModel
				{
					ProcessName = parameters.ProcessName ?? string.Empty,
					Arguments = parameters.Arguments ?? [],
					Command = parameters.Command ?? string.Empty,
					WorkingDirectory = parameters.WorkingDirectory,
				};

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
					result.ResultContent = viewModel.Output ?? string.Empty;
					result.CompleteWithError();
					return;
				}

				// Return result based on exit code
				if (exitCode == 0)
				{
					result.ResultContent = viewModel.Output ?? string.Empty;
					result.CompleteWithSuccess();
					return;
				}
				else
				{
					result.ResultContent = viewModel.Output +
						$"\nProcess exited with code {exitCode}. Check terminal output above for details.";
					result.CompleteWithSuccess();
					return;
				}
			}, cancellationToken);

			return result;

		}
	}
}
