using LLMDesktopAssistant.Addons.Loading;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Addons.Management
{
	[Service(typeof(IAddonManagerInvalidator))]
	public class AppAddonManagerInvalidator(
		IAppAddonManager appAddonManager,
		IAppAddonPackLocator appAddonPackLocator,
		IEnumerable<IAddonManagerInvalidationHook> hooks
	) : IAddonManagerInvalidator
	{
		public void Reload(AddonKind kinds)
		{
			foreach (var hook in hooks)
				hook.ReloadRequested(kinds, force: true);

			appAddonPackLocator.Invalidate();
			appAddonManager.Reload(kinds);

			foreach (var hook in hooks)
				hook.Reloaded(kinds, force: true);
		}

		public void Invalidate(AddonKind kinds)
		{
			foreach (var hook in hooks)
				hook.InvalidationRequested(kinds);

			appAddonPackLocator.Invalidate();
			appAddonManager.Invalidate(kinds);
		}

		public void ReloadIfInvalid(AddonKind kinds)
		{
			foreach (var hook in hooks)
				hook.ReloadRequested(kinds, force: false);

			if (appAddonManager.ReloadIfInvalid(kinds))
				foreach (var hook in hooks)
					hook.Reloaded(kinds, force: false);
		}

		public void InvalidateOnFileChange(string path)
		{
			// TODO: Implement logic to check if the path touches addon files.
			// For now, just invalidate.
			Invalidate(AddonKind.All);
		}
	}
}
