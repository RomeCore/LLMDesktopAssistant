namespace LLMDesktopAssistant.Addons.Management
{
	public interface IAddonManagerInvalidator
	{
		/// <summary>
		/// Reloads the managers regardless of their current state.
		/// </summary>
		void Reload();

		/// <summary>
		/// Invalidates the managers if the specified path potentially touches addon files.
		/// </summary>
		void Invalidate();

		/// <summary>
		/// Reloads the managers if they are currently invalid.
		/// </summary>
		void ReloadIfInvalid();

		/// <summary>
		/// Invalidates the managers if the specified path potentially touches addon files.
		/// </summary>
		void InvalidateOnFileChange(string path);
	}
}
