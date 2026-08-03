using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Desktop.Execution;
using LLMDesktopAssistant.LLM.MVVM.Additional;
using LLMDesktopAssistant.Utils;

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
		[ChangeTracker.Untracked]
		public Task<int> ExitCodeTask => _exitCodeTcs.Task;

		private ProcessDescriptor? _descriptor;
		/// <summary>
		/// The process descriptor created by the <see cref="IProcessLauncher"/> for this session.
		/// Its <see cref="ProcessTerminalSession"/> is rendered by the view.
		/// </summary>
		public ProcessDescriptor? Descriptor
		{
			get => _descriptor;
			set => SetProperty(ref _descriptor, value);
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
				try
				{
					_cts?.Cancel();
					_cts?.Dispose();
				}
				catch { }
			}
		}
	}
}
