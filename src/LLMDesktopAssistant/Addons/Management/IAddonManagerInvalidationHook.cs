namespace LLMDesktopAssistant.Addons.Management
{
	public interface IAddonManagerInvalidationHook
	{
		/// <summary>
		/// Called when a reload is requested.
		/// </summary>
		/// <param name="force">Whether the reload is forced (ignoring invalid state).</param>
		void ReloadRequested(bool force);

		/// <summary>
		/// Called when an invalidation is requested.
		/// </summary>
		void InvalidationRequested();

		/// <summary>
		/// Called when a reload has completed.
		/// </summary>
		/// <param name="force">Whether the reload was forced (not invalid before).</param>
		void Reloaded(bool force);
	}
}
