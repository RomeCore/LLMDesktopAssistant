using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Services.Instances;

namespace LLMDesktopAssistant.Addons.Management
{
	[ChatService]
	public class ApplicationActivationWatcher : Disposable
	{
		public ApplicationActivationWatcher(IAddonManagerInvalidator invalidator,
			IApplicationViewActivationEvents? evt = null)
		{
			if (evt is null)
				return;

			void AppActivated()
			{
				invalidator.Invalidate(AddonKind.All);
			}

			evt.Activated += AppActivated;

			OnDispose += (s, e) =>
			{
				evt.Activated -= AppActivated;
			};
		}
	}
}
