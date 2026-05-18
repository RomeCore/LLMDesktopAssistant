using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.MVVM;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Desktop.ToolModules.Terminal
{
	/// <summary>
	/// ViewModel for displaying a terminal emulator inside a chat message.
	/// Uses Iciclecreek.Avalonia.Terminal to show live process output.
	/// </summary>
	[ViewModelFor(typeof(TerminalAdditionalView))]
	public class TerminalAdditionalViewModel : AdditionalMessageViewModel
	{
		private readonly TaskCompletionSource<int> _exitCodeTcs = new();

		/// <summary>
		/// Task that completes when the process exits, returning the exit code.
		/// </summary>
		public Task<int> ExitCodeTask => _exitCodeTcs.Task;

		private string _processName = string.Empty;
		/// <summary>
		/// The name of the process to run (e.g., "python", "cmd.exe", "bash").
		/// </summary>
		public string ProcessName
		{
			get => _processName;
			set => SetProperty(ref _processName, value);
		}

		private string[] _arguments = [];
		/// <summary>
		/// Arguments for the process.
		/// </summary>
		public string[] Arguments
		{
			get => _arguments;
			set => SetProperty(ref _arguments, value);
		}

		private string? _workingDirectory;
		/// <summary>
		/// The working directory for the process.
		/// </summary>
		public string? WorkingDirectory
		{
			get => _workingDirectory;
			set => SetProperty(ref _workingDirectory, value);
		}

		private string _command = string.Empty;
		/// <summary>
		/// The command line to execute via shell (used when ProcessName is not specified).
		/// On Windows runs via cmd.exe /c, on Linux via bash -c.
		/// </summary>
		public string Command
		{
			get => _command;
			set => SetProperty(ref _command, value);
		}

		private CancellationToken _cancellationToken = default;
		/// <summary>
		/// The cancellation token to use to kill the process.
		/// </summary>
		public CancellationToken CancellationToken
		{
			get => _cancellationToken;
			set => SetProperty(ref _cancellationToken, value);
		}

		private bool _isRunning;
		/// <summary>
		/// Whether the process is currently running.
		/// </summary>
		public bool IsRunning
		{
			get => _isRunning;
			set => SetProperty(ref _isRunning, value);
		}

		private bool _isCompleted;
		/// <summary>
		/// Whether the process has completed.
		/// </summary>
		public bool IsCompleted
		{
			get => _isCompleted;
			set => SetProperty(ref _isCompleted, value);
		}

		private int _exitCode;
		/// <summary>
		/// Exit code of the process.
		/// </summary>
		public int ExitCode
		{
			get => _exitCode;
			set => SetProperty(ref _exitCode, value);
		}

		private string? _output;
		/// <summary>
		/// The output of the process.
		/// </summary>
		public string? Output
		{
			get => _output;
			set => SetProperty(ref _output, value);
		}

		private string? _errorMessage;
		/// <summary>
		/// Error message if process failed to launch.
		/// </summary>
		public string? ErrorMessage
		{
			get => _errorMessage;
			set => SetProperty(ref _errorMessage, value);
		}

		/// <summary>
		/// Cancels the running process.
		/// </summary>
		public IRelayCommand CancelCommand { get; }

		private CancellationTokenSource? _cts;

		public TerminalAdditionalViewModel()
		{
			CancelCommand = new RelayCommand(Cancel);
			IsTemporary = true; // Don't persist terminal sessions in DB
		}

		/// <summary>
		/// Cancels the process via CancellationTokenSource.
		/// </summary>
		public void Cancel()
		{
			_cts?.Cancel();
		}

		/// <summary>
		/// Sets the CancellationTokenSource used to cancel the process.
		/// Called by the view when it's loaded.
		/// </summary>
		public void SetCancellationTokenSource(CancellationTokenSource cts)
		{
			_cts = cts;
		}

		/// <summary>
		/// Marks the process as completed with the given exit code.
		/// </summary>
		public void Complete(int exitCode)
		{
			IsRunning = false;
			IsCompleted = true;
			ExitCode = exitCode;
			_exitCodeTcs.TrySetResult(exitCode);
		}

		/// <summary>
		/// Marks the process as failed with an error message.
		/// </summary>
		public void Fail(string error)
		{
			IsRunning = false;
			IsCompleted = true;
			ErrorMessage = error;
			_exitCodeTcs.TrySetResult(-1);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				_cts?.Cancel();
				_cts?.Dispose();
			}
		}
	}
}
