using LLMDesktopAssistant.LLM.Services;

namespace LLMDesktopAssistant.Addons.Management
{
	[ChatService(typeof(IAddonManagerInvalidator))]
	public class ChatAddonManagerInvalidator(
		IAppAddonManager appAddonManager,
		IChatAddonManager chatAddonManager
	) : IAddonManagerInvalidator
	{
		public void Reload()
		{
			appAddonManager.Reload();
			chatAddonManager.Reload();
		}

		public void Invalidate()
		{
			appAddonManager.Invalidate();
			chatAddonManager.Invalidate();
		}

		public void ReloadIfInvalid()
		{
			appAddonManager.ReloadIfInvalid();
			chatAddonManager.ReloadIfInvalid();
		}

		public void InvalidateOnFileChange(string path)
		{
			// TODO: Implement logic to check if the path touches addon files.
			// For now, just invalidate.
			Invalidate();
		}
	}
}
