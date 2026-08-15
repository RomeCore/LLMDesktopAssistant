using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LLMDesktopAssistant.Desktop.Execution;

namespace LLMDesktopAssistant.Desktop.ToolModules.Terminal
{
	/// <summary>
	/// View for displaying a terminal emulator inside a chat message.
	/// Renders the <see cref="ProcessTerminalSession"/> created by the launcher:
	/// the view only displays the terminal buffer and forwards user input to the PTY.
	/// </summary>
	public partial class TerminalAdditionalView : UserControl
	{
		private TerminalAdditionalViewModel? _viewModel;
		private bool _isRunning;

		public TerminalAdditionalView()
		{
			InitializeComponent();

			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
			DataContextChanged += OnDataContextChanged;
		}

		private void OnDataContextChanged(object? sender, EventArgs e)
		{
			_viewModel = DataContext as TerminalAdditionalViewModel;
		}

		private void OnLoaded(object? sender, RoutedEventArgs e)
		{
			if (_viewModel == null || TerminalHost == null || _isRunning)
				return;

			var session = _viewModel.Descriptor?.TerminalSession;
			if (session == null)
			{
				_viewModel.Fail("No terminal session available.");
				return;
			}

			_isRunning = true;

			// Attach the session to the view. The launcher reads the PTY and writes to the
			// terminal; the view renders the buffer and forwards input back to the PTY.
			TerminalHost.Terminal = session.Terminal;
			TerminalHost.PtyConnection = session.Pty;

			// The launcher raises OutputUpdated when new data is written to the terminal
			// (XTerm.Terminal.BufferChanged only fires on buffer switches), so repaint on it.
			session.OutputUpdated += OnSessionOutputUpdated;
		}

		private void OnUnloaded(object? sender, RoutedEventArgs e)
		{
			_isRunning = false;

			if (_viewModel?.Descriptor?.TerminalSession is { } session)
			{
				session.OutputUpdated -= OnSessionOutputUpdated;
			}
		}

		private void OnSessionOutputUpdated(object? sender, EventArgs e)
		{
			Dispatcher.UIThread.Post(() => TerminalHost?.InvalidateVisual());
		}
	}
}
