using LLMDesktopAssistant.LLM.Services;

namespace LLMDesktopAssistant.Addons.Management
{
	[ChatService(typeof(IAddonManagerInvalidator))]
	public class ChatAddonManagerInvalidator(
		IAppAddonManager appAddonManager,
		IChatAddonManager chatAddonManager,
		IEnumerable<IAddonManagerInvalidationHook> hooks
	) : IAddonManagerInvalidator
	{
		public void Reload()
		{
			foreach (var hook in hooks)
				hook.ReloadRequested(force: true);

			appAddonManager.Reload();
			chatAddonManager.Reload();

			foreach (var hook in hooks)
				hook.Reloaded(force: true);
		}

		public void Invalidate()
		{
			foreach (var hook in hooks)
				hook.InvalidationRequested();

			appAddonManager.Invalidate();
			chatAddonManager.Invalidate();
		}

		public void ReloadIfInvalid()
		{
			foreach (var hook in hooks)
				hook.ReloadRequested(force: false);

			var appReloaded = appAddonManager.ReloadIfInvalid();
			var chatReloaded = chatAddonManager.ReloadIfInvalid();

			if (appReloaded || chatReloaded)
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
