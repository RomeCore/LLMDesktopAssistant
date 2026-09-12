using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Addons.Management
{
	[Service(typeof(IAddonManagerInvalidator))]
	public class AppAddonManagerInvalidator(
		IAppAddonManager appAddonManager,
		IEnumerable<IAddonManagerInvalidationHook> hooks
	) : IAddonManagerInvalidator
	{
		public void Reload()
		{
			foreach (var hook in hooks)
				hook.ReloadRequested(force: true);

			appAddonManager.Reload();

			foreach (var hook in hooks)
				hook.Reloaded(force: true);
		}

		public void Invalidate()
		{
			foreach (var hook in hooks)
				hook.InvalidationRequested();

			appAddonManager.Invalidate();
		}

		public void ReloadIfInvalid()
		{
			foreach (var hook in hooks)
				hook.ReloadRequested(force: false);

			if (appAddonManager.ReloadIfInvalid())
				foreach (var hook in hooks)
					hook.Reloaded(force: false);
		}

		public void InvalidateOnFileChange(string path)
		{
			// TODO: Implement logic to check if the path touches addon files.
			// For now, just invalidate.
			Invalidate();
		}
	}
}
