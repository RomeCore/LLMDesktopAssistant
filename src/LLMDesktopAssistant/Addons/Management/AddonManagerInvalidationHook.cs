using LLMDesktopAssistant.LLM.Services;

namespace LLMDesktopAssistant.Addons.Management
{
	[ChatService(typeof(IAddonManagerInvalidationHook))]
	[ChatService(typeof(AddonManagerInvalidationHook))]
	public class AddonManagerInvalidationHook : IAddonManagerInvalidationHook
	{
		public event Action<AddonKind, bool>? OnReloadRequested;

		public event Action<AddonKind>? OnInvalidationRequested;

		public event Action<AddonKind, bool>? OnReloaded;

		public void ReloadRequested(AddonKind kinds, bool force)
		{
			OnReloadRequested?.Invoke(kinds, force);
		}

		public void InvalidationRequested(AddonKind kinds)
		{
			OnInvalidationRequested?.Invoke(kinds);
		}

		public void Reloaded(AddonKind kinds, bool force)
		{
			OnReloaded?.Invoke(kinds, force);
		}
	}
}
