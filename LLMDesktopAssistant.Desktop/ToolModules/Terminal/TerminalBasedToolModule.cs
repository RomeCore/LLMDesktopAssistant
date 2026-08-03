using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;
using LLMDesktopAssistant.Desktop.Execution;
using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.Desktop.ToolModules.Terminal
{
	/// <summary>
	/// Base class for tool modules that run processes and display their output
	/// in a terminal emulator embedded in the chat message.
	/// </summary>
	public abstract class TerminalBasedToolModule : ToolModule
	{
		private readonly IProcessLauncher _processLauncher;

		/// <summary>
		/// Initializes a new instance of the <see cref="TerminalBasedToolModule"/> class.
		/// </summary>
		/// <param name="processLauncher">The process launcher used to start child processes.</param>
		protected TerminalBasedToolModule(IProcessLauncher processLauncher)
		{
			_processLauncher = processLauncher;
		}

		/// <summary>
		/// Runs a process with terminal output displayed in the chat message.
		/// Creates a <see cref="TerminalAdditionalViewModel"/>, adds it to the message's
		/// AdditionalViewModels collection, and waits for the process to complete.
		/// </summary>
		/// <param name="parameters">Parameters describing what to run.</param>
		/// <param name="context">The tool execution context (provides access to the chat message).</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>A ReactiveToolResult with the process exit code.</returns>
		protected async Task<ReactiveToolResult> RunAsync(
			TerminalToolRunParameters parameters,
			ToolExecutionContext context,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(parameters);
			ArgumentNullException.ThrowIfNull(context);

			var message = context.Message;

			var result = new ReactiveToolResult
			{
				StatusIcon = parameters.StatusIcon,
				StatusTitle = parameters.StatusTitle
			};

			var (process, args) = ResolveProcessAndArgs(parameters);

			ProcessDescriptor descriptor;
			try
			{
				descriptor = _processLauncher.Launch(new ProcessLaunchParameters
				{
					ProcessName = process,
					FileName = process,
					Arguments = args.ToImmutableList(),
					WorkingDirectory = parameters.WorkingDirectory ?? Environment.CurrentDirectory,
					RunInTerminal = parameters.RunTerminal
				}, cancellationToken);
			}
			catch (Exception ex)
			{
				result.ResultContent = $"Failed to launch process: {ex.Message}";
				result.CompleteWithError();
				return result;
			}

			TerminalAdditionalViewModel? viewModel = null;
			if (parameters.RunTerminal)
			{
				viewModel = new TerminalAdditionalViewModel
				{
					Descriptor = descriptor,
					IsRunning = true
				};
				viewModel.SetCancellationTokenSource(descriptor.CancellationTokenSource);
				message.AdditionalViewModels.Add(viewModel);
			}

			int exitCode;
			try
			{
				exitCode = await descriptor.ExitCodeTask.WaitAsync(cancellationToken);
			}
			catch (OperationCanceledException)
			{
				viewModel?.Cancel();
				result.ResultContent = BuildOutput(descriptor);
				result.CompleteWithError();
				return result;
			}

			viewModel?.Complete(exitCode);
			result.ResultContent = BuildOutput(descriptor);

			if (exitCode == 0)
			{
				result.CompleteWithSuccess();
			}
			else
			{
				result.ResultContent += $"\nProcess exited with code {exitCode}. Check terminal output above for details.";
				result.CompleteWithError();
			}

			return result;
		}

		private static string BuildOutput(ProcessDescriptor descriptor)
		{
			var lines = descriptor.TerminalSession?.Output ?? descriptor.Output?.Output;
			if (lines == null || lines.Count == 0)
				return string.Empty;

			return string.Join(Environment.NewLine, lines);
		}

		private static (string Process, string[] Args) ResolveProcessAndArgs(TerminalToolRunParameters parameters)
		{
			if (!string.IsNullOrEmpty(parameters.ProcessName))
			{
				// Explicit process specified
				return (parameters.ProcessName, parameters.Arguments ?? []);
			}
			else if (!string.IsNullOrEmpty(parameters.Command))
			{
				// Run command via system shell
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					return ("cmd.exe", ["/c " + parameters.Command]);
				else
					return ("/bin/bash", ["-c " + parameters.Command]);
			}
			else
			{
				// Default: open interactive shell
				return (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "bash", []);
			}
		}
	}
}
