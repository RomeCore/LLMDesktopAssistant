namespace LLMDesktopAssistant.Addons.Management
{
	public interface IAddonManagerInvalidationHook
	{
		/// <summary>
		/// Called when a reload is requested.
		/// </summary>
		/// <param name="kinds">What addon kinds was requested for reload.</param>
		/// <param name="force">Whether the reload is forced (ignoring invalid state).</param>
		void ReloadRequested(AddonKind kinds, bool force);

		/// <summary>
		/// Called when an invalidation is requested.
		/// </summary>
		/// <param name="kinds">What addon kinds was requested for invalidation.</param>
		void InvalidationRequested(AddonKind kinds);

		/// <summary>
		/// Called when a reload has completed.
		/// </summary>
		/// <param name="kinds">What addon kinds was reloaded.</param>
		/// <param name="force">Whether the reload was forced (not invalid before).</param>
		void Reloaded(AddonKind kinds, bool force);
	}
}
