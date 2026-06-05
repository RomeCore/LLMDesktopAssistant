using LLMDesktopAssistant.Controls.Toasts;

namespace LLMDesktopAssistant.Services.Instances
{
	public interface IToastService
	{
		/// <summary>
		/// Shows an info toast.
		/// </summary>
		void ShowInfo(string title, string? description = null, double durationSeconds = 5);

		/// <summary>
		/// Shows a warning toast.
		/// </summary>
		void ShowWarning(string title, string? description = null, double durationSeconds = 6);

		/// <summary>
		/// Shows an error toast.
		/// </summary>
		void ShowError(string title, string? description = null, double durationSeconds = 8);

		/// <summary>
		/// Shows a success toast.
		/// </summary>
		void ShowSuccess(string title, string? description = null, double durationSeconds = 5);

		/// <summary>
		/// Shows a custom toast.
		/// </summary>
		void Show(ToastItemViewModel toast);

		/// <summary>
		/// Dismisses a specific toast by its ID.
		/// </summary>
		void Dismiss(long toastId);

		/// <summary>
		/// Dismisses all currently visible toasts.
		/// </summary>
		void DismissAll();
	}
}