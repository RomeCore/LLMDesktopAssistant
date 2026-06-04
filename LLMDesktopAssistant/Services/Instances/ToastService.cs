using Avalonia.Controls;
using Avalonia.Threading;
using LLMDesktopAssistant.Controls.Toasts;

namespace LLMDesktopAssistant.Services.Instances;

/// <summary>
/// Global service for showing toast notifications from anywhere in the application.
/// Connects to the <see cref="ToastControl"/> instance hosted in <see cref="MVVM.MainView"/>.
/// </summary>
[Service]
public class ToastService
{
	private ToastControl? _toastControl;

	/// <summary>
	/// Gets or sets the ToastControl instance to use for displaying toasts.
	/// This is automatically set by <see cref="MVVM.MainView"/> on load.
	/// </summary>
	public ToastControl? ToastControl
	{
		get => _toastControl;
		set
		{
			if (_toastControl != null && value != null && _toastControl != value)
			{
				// Transfer any queued toasts from the old control to the new one
				foreach (var toast in _toastControl.Toasts.ToList())
				{
					_toastControl.Toasts.Remove(toast);
					value.Toasts.Add(toast);
				}
			}

			_toastControl = value;
		}
	}

	// ── Show Methods ─────────────────────────────────────────────────────

	/// <summary>
	/// Shows an info toast.
	/// </summary>
	public void ShowInfo(string title, string? description = null, double durationSeconds = 5.0)
	{
		ExecuteOnControl(tc => tc.ShowInfo(title, description, durationSeconds));
	}

	/// <summary>
	/// Shows a warning toast.
	/// </summary>
	public void ShowWarning(string title, string? description = null, double durationSeconds = 6.0)
	{
		ExecuteOnControl(tc => tc.ShowWarning(title, description, durationSeconds));
	}

	/// <summary>
	/// Shows an error toast.
	/// </summary>
	public void ShowError(string title, string? description = null, double durationSeconds = 8.0)
	{
		ExecuteOnControl(tc => tc.ShowError(title, description, durationSeconds));
	}

	/// <summary>
	/// Shows a success toast.
	/// </summary>
	public void ShowSuccess(string title, string? description = null, double durationSeconds = 5.0)
	{
		ExecuteOnControl(tc => tc.ShowSuccess(title, description, durationSeconds));
	}

	/// <summary>
	/// Shows a custom toast.
	/// </summary>
	public void Show(ToastItemViewModel toast)
	{
		ExecuteOnControl(tc => tc.Show(toast));
	}

	/// <summary>
	/// Dismisses a specific toast by its ID.
	/// </summary>
	public void Dismiss(long toastId)
	{
		ExecuteOnControl(tc => tc.Dismiss(toastId));
	}

	/// <summary>
	/// Dismisses all currently visible toasts.
	/// </summary>
	public void DismissAll()
	{
		ExecuteOnControl(tc => tc.DismissAll());
	}

	// ── Private Helpers ──────────────────────────────────────────────────

	private void ExecuteOnControl(Action<ToastControl> action)
	{
		if (_toastControl == null)
			return;

		if (Dispatcher.UIThread.CheckAccess())
		{
			action(_toastControl);
		}
		else
		{
			Dispatcher.UIThread.Post(() => action(_toastControl));
		}
	}
}
