using System.Windows.Input;

namespace LLMDesktopAssistant.Controls.Toasts;

/// <summary>
/// ViewModel representing a single toast notification.
/// </summary>
public class ToastItemViewModel : NotifyPropertyChanged
{
	private static long _nextId;

	/// <summary>
	/// Unique identifier for this toast instance.
	/// </summary>
	public long Id { get; } = Interlocked.Increment(ref _nextId);

	/// <summary>
	/// The type of the toast (determines icon and color).
	/// </summary>
	public required ToastType Type { get; init; }

	/// <summary>
	/// The title text of the toast. Supports Markdown.
	/// </summary>
	public required string Title { get; init; }

	/// <summary>
	/// Optional description text. Supports Markdown.
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// Command invoked when the user clicks the dismiss (✕) button.
	/// The command parameter is the toast's <see cref="Id"/>.
	/// </summary>
	public required ICommand DismissCommand { get; init; }

	/// <summary>
	/// Duration in seconds before the toast auto-dismisses.
	/// Use 0 or negative for a sticky toast that requires manual dismiss.
	/// </summary>
	public double DurationSeconds { get; init; } = 5.0;

	/// <summary>
	/// Whether to render the toast content as Markdown.
	/// </summary>
	public bool UseMarkdown { get; init; } = true;

	/// <summary>
	/// Whether the toast is currently visible (used for animations).
	/// </summary>
	private bool _isVisible = true;
	public bool IsVisible
	{
		get => _isVisible;
		set => SetProperty(ref _isVisible, value);
	}
}
