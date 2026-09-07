namespace LLMDesktopAssistant.Addons.Management
{
	/// <summary>
	/// Manages the loading of addons for the application.
	/// </summary>
	public interface IAddonManager
	{
		/// <summary>
		/// Forces the manager to reload all addons.
		/// </summary>
		void Reload();

		/// <summary>
		/// Marks the manager state as invalid (dirty), indicating that it needs to reload all addons.
		/// </summary>
		void Invalidate();

		/// <summary>
		/// Reloads all addons if manager state is invalid (i.e., needs to reload) without forcing a reload.
		/// </summary>
		void ReloadIfInvalid();
	}
}
