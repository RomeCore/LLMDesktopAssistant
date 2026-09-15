namespace LLMDesktopAssistant.Addons.Management
{
	/// <summary>
	/// Manages the loading of addons for the application.
	/// </summary>
	public interface IAddonManager
	{
		/// <summary>
		/// Forces the manager to reload all addons of specified kinds.
		/// </summary>
		void Reload(AddonKind kinds);

		/// <summary>
		/// Marks the manager state as invalid (dirty), indicating that it needs to reload all addons of specified kinds.
		/// </summary>
		void Invalidate(AddonKind kinds);

		/// <summary>
		/// Reloads all addons of specified kinds if manager state is invalid (i.e., needs to reload) without forcing a reload.
		/// </summary>
		/// <returns><see langword="true"/> if the manager state was invalid and a reload was performed; <see langword="false"/> otherwise.</returns>
		bool ReloadIfInvalid(AddonKind kinds);
	}
}
