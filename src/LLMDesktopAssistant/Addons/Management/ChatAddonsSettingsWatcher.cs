using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Settings.Application;

namespace LLMDesktopAssistant.Addons.Management
{
	[ChatService]
	public class ChatAddonsSettingsWatcher : Disposable
	{
		public ChatAddonsSettingsWatcher(IChatSettingsService chatSettings, IAddonManagerInvalidator invalidator)
		{
			var appAddonsSettings = ApplicationSettingsAccessor.ApplicationSettings.InheritedChatSettings.Addons;
			var addonsSettings = chatSettings.Settings.Addons;

			void AddonsSettings_DeepChanged(object? sender, EventArgs e)
			{
				invalidator.Invalidate();
			}

			appAddonsSettings.DeepChanged += AddonsSettings_DeepChanged;
			addonsSettings.DeepChanged += AddonsSettings_DeepChanged;

			OnDispose += (s, e) =>
			{
				appAddonsSettings.DeepChanged -= AddonsSettings_DeepChanged;
				addonsSettings.DeepChanged -= AddonsSettings_DeepChanged;
			};
		}
	}
}
