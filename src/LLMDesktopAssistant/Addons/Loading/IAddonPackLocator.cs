namespace LLMDesktopAssistant.Addons.Loading
{
	public interface IAddonPackLocator
	{
		/// <summary>
		/// Invalidates the internal pack cache, causing to recompute for next retrieval.
		/// </summary>
		void Invalidate();

		/// <summary>
		/// Gets all addon packs information, even not configurable packs.
		/// Used mostly for UI purposes.
		/// </summary>
		IEnumerable<AddonPackInfo> GetAllPacks();

		/// <summary>
		/// Gets all effective addon packs information. These packs are currently enabled and can be used.
		/// </summary>
		IEnumerable<AddonPackInfo> GetEffectivePacks();
	}
}
