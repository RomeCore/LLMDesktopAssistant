namespace LLMDesktopAssistant.Addons.Loading
{
	public interface IAddonPackLocator
	{
		/// <summary>
		/// Gets all addon packs information, even not configurable (implicit) packs.
		/// Used mostly for UI purposes.
		/// </summary>
		IEnumerable<AddonPackInfo> GetAllPacks();

		/// <summary>
		/// Gets all effective addon packs information. These packs are currently enabled and can be used.
		/// </summary>
		IEnumerable<AddonPackInfo> GetEffectivePacks();
	}
}
