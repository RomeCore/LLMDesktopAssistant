using LLMDesktopAssistant.Addons.Loading;
using LLMDesktopAssistant.LLM.Services;

namespace LLMDesktopAssistant.Addons.Management
{
	[ChatService(typeof(IAddonManagerInvalidator))]
	public class ChatAddonManagerInvalidator(
		IAppAddonManager appAddonManager,
		IChatAddonManager chatAddonManager,
		IAppAddonPackLocator appAddonPackLocator,
		IChatAddonPackLocator chatAddonPackLocator,
		IEnumerable<IAddonManagerInvalidationHook> hooks
	) : IAddonManagerInvalidator
	{
		public void Reload(AddonKind kinds)
		{
			foreach (var hook in hooks)
				hook.ReloadRequested(kinds, force: true);

			if (kinds.HasFlag(AddonKind.Pack))
			{
				appAddonPackLocator.Invalidate();
				chatAddonPackLocator.Invalidate();
			}
			appAddonManager.Reload(kinds);
			chatAddonManager.Reload(kinds);

			foreach (var hook in hooks)
				hook.Reloaded(kinds, force: true);
		}

		public void Invalidate(AddonKind kinds)
		{
			foreach (var hook in hooks)
				hook.InvalidationRequested(kinds);

			if (kinds.HasFlag(AddonKind.Pack))
			{
				appAddonPackLocator.Invalidate();
				chatAddonPackLocator.Invalidate();
			}
			appAddonManager.Invalidate(kinds);
			chatAddonManager.Invalidate(kinds);
		}

		public void ReloadIfInvalid(AddonKind kinds)
		{
			foreach (var hook in hooks)
				hook.ReloadRequested(kinds, force: false);

			var appReloaded = appAddonManager.ReloadIfInvalid(kinds);
			var chatReloaded = chatAddonManager.ReloadIfInvalid(kinds);

			if (appReloaded || chatReloaded)
				foreach (var hook in hooks)
					hook.Reloaded(kinds, force: false);
		}
	}
}
