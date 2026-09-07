using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Addons.Management
{
	[Service(typeof(IAddonManagerInvalidator))]
	public class AppAddonManagerInvalidator(
		IAppAddonManager appAddonManager
	) : IAddonManagerInvalidator
	{
		public void Reload()
		{
			appAddonManager.Reload();
		}

		public void Invalidate()
		{
			appAddonManager.Invalidate();
		}

		public void ReloadIfInvalid()
		{
			appAddonManager.ReloadIfInvalid();
		}

		public void InvalidateOnFileChange(string path)
		{
			// TODO: Implement logic to check if the path touches addon files.
			// For now, just invalidate.
			Invalidate();
		}
	}
}
