using LLMDesktopAssistant.LLM.Services;

namespace LLMDesktopAssistant.Addons.Management
{
	[ChatService(typeof(IAddonManagerInvalidationHook))]
	[ChatService(typeof(AddonManagerInvalidationHook))]
	public class AddonManagerInvalidationHook : IAddonManagerInvalidationHook
	{
		public event Action<bool>? OnReloadRequested;

		public event Action? OnInvalidationRequested;

		public event Action<bool>? OnReloaded;

		public void ReloadRequested(bool force)
		{
			OnReloadRequested?.Invoke(force);
		}

		public void InvalidationRequested()
		{
			OnInvalidationRequested?.Invoke();
		}

		public void Reloaded(bool force)
		{
			OnReloaded?.Invoke(force);
		}
	}
}
