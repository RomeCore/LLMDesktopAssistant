using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Iciclecreek.Terminal;
using System;
using System.Linq;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Diagnostics;

namespace LLMDesktopAssistant.Desktop.ToolModules.Terminal
{
	public partial class TerminalAdditionalView : UserControl
	{
		private TerminalAdditionalViewModel? _viewModel;
		private CancellationTokenSource? _cts;
		private IDisposable? _killUnsub;

		public TerminalAdditionalView()
		{
			InitializeComponent();

			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
			DataContextChanged += OnDataContextChanged;
		}

		private void OnDataContextChanged(object? sender, EventArgs e)
		{
			if (_viewModel != null)
			{
				_viewModel.PropertyChanged -= OnViewModelPropertyChanged;
			}

			_viewModel = DataContext as TerminalAdditionalViewModel;

			if (_viewModel != null)
			{
				_viewModel.PropertyChanged += OnViewModelPropertyChanged;
			}
		}

		private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			// React to ViewModel state changes if needed
		}

		private async void OnLoaded(object? sender, RoutedEventArgs e)
		{
			if (_viewModel == null || Terminal == null)
				return;

			_cts = CancellationTokenSource.CreateLinkedTokenSource(_viewModel.CancellationToken);
			_viewModel.SetCancellationTokenSource(_cts);

			// Determine what to launch
			string process;
			string[] args;
			string? workDir = _viewModel.WorkingDirectory;

			if (!string.IsNullOrEmpty(_viewModel.ProcessName))
			{
				// Explicit process specified
				process = _viewModel.ProcessName;
				args = _viewModel.Arguments ?? [];
			}
			else if (!string.IsNullOrEmpty(_viewModel.Command))
			{
				// Run command via system shell
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					process = "cmd.exe";
					args = ["/c " + _viewModel.Command ];
				}
				else
				{
					process = "/bin/bash";
					args = ["-c " + _viewModel.Command];
				}
			}
			else
			{
				// Default: open interactive shell
				process = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "bash";
				args = [];
			}

			_viewModel.IsRunning = true;

			try
			{
				// Subscribe to process exit
				Terminal.ProcessExited += OnProcessExited;

				// Launch the process in the terminal
				await Terminal.LaunchProcess(workDir, process, args);

				_killUnsub = _cts.Token.Register(async () =>
				{
					try
					{
						Process? process = null;
						try
						{
							process = Process.GetProcessById(Terminal.Pid);
						}
						catch (ArgumentException)
						{
						}
						Terminal.Kill();
						await Task.Delay(200);
						process?.Kill();
					}
					catch
					{
					}
					_killUnsub = null;
				});
			}
			catch (Exception ex)
			{
				_viewModel.Fail($"Failed to launch process: {ex.Message}");
			}
		}

		private void OnProcessExited(object? sender, ProcessExitedEventArgs e)
		{
			// Unsubscribe from event
			Terminal.ProcessExited -= OnProcessExited;

			// Update ViewModel on UI thread
			Dispatcher.UIThread.Post(async () =>
			{
				await Task.Delay(100);

				var buffer = Terminal.Terminal.Buffer;
				var sb = new StringBuilder();
				for (int i = 0; i < buffer.Lines.Length; i++)
				{
					var line = buffer.Lines[i];
					if (line == null)
					{
						if (i < buffer.Lines.Length - 1)
							sb.AppendLine();
						continue;
					}
					for (int j = 0; j < line.Length; j++)
					{
						var cell = line[j];
						sb.Append(cell.Content);
					}
					if (i < buffer.Lines.Length - 1)
						sb.AppendLine();
				}
				_viewModel?.Output = sb.ToString();
				_viewModel?.Complete(e.ExitCode);
			});
		}

		private void OnUnloaded(object? sender, RoutedEventArgs e)
		{
			try
			{
				// Clean up
				_cts?.Cancel();
				_cts?.Dispose();
				_cts = null;
				_killUnsub?.Dispose();
				_killUnsub = null;
			}
			catch
			{
			}

			if (Terminal != null)
			{
				Terminal.ProcessExited -= OnProcessExited;

				try
				{
					Terminal.Kill();
				}
				catch
				{
					// Ignore errors during cleanup
				}
			}
		}
	}
}
