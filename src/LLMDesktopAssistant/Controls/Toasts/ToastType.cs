namespace LLMDesktopAssistant.Controls.Toasts;

/// <summary>
/// Defines the type of a toast notification, which determines its icon and color scheme.
/// </summary>
public enum ToastType
{
	/// <summary>
	/// Informational toast (neutral/primary color scheme).
	/// </summary>
	Info,

	/// <summary>
	/// Warning toast (yellow/amber color scheme).
	/// </summary>
	Warning,

	/// <summary>
	/// Error toast (red color scheme).
	/// </summary>
	Error,

	/// <summary>
	/// Success toast (green color scheme).
	/// </summary>
	Success,
}
