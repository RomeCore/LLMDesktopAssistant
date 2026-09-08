using LLMDesktopAssistant.LLM.Services;

namespace LLMDesktopAssistant.Addons.Loading
{
	[ChatService(typeof(IAddonPackLocator))]
	public class CombinedAddonPackLocator(
		IAppAddonPackLocator appLocator,
		IChatAddonPackLocator chatLocator
	) : IAddonPackLocator
	{
		public IEnumerable<AddonPackInfo> GetAllPacks()
		{
			return appLocator.GetAllPacks().Concat(chatLocator.GetAllPacks());
		}

		public IEnumerable<AddonPackInfo> GetEffectivePacks()
		{
			return appLocator.GetEffectivePacks().Concat(chatLocator.GetEffectivePacks());
		}
	}
}
