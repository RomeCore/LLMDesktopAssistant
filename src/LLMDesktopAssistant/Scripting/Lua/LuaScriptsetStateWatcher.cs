using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Addons.Management;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Settings.Application;

namespace LLMDesktopAssistant.Scripting.Lua
{
	[ChatService(typeof(ILuaScriptsetStateWatcher))]
	public class LuaScriptsetStateWatcher : Disposable, ILuaScriptsetStateWatcher
	{
		private readonly IAddonSetCollector<LuaScriptInfo> _scriptsetCollector;
		private readonly IChatSettingsService _chatSettings;
		private readonly AddonManagerInvalidationHook _addonHook;

		public event Action? OnStateChanged;

		public LuaScriptsetStateWatcher(IAddonSetCollector<LuaScriptInfo> scriptsetCollector,
			IChatSettingsService chatSettings, AddonManagerInvalidationHook addonHook)
		{
			_scriptsetCollector = scriptsetCollector;
			_chatSettings = chatSettings;
			_addonHook = addonHook;

			_chatSettings.Settings.Scripts.DeepChanged += ScriptsSettings_DeepChanged;
			ApplicationSettingsAccessor.ApplicationSettings.InheritedChatSettings.Scripts.DeepChanged += ScriptsSettings_DeepChanged;
			_addonHook.OnReloaded += AddonHook_OnReloaded;
		}

		private void ScriptsSettings_DeepChanged(object? sender, EventArgs e)
		{
			OnStateChanged?.Invoke();
		}

		private void AddonHook_OnReloaded(bool obj)
		{
			OnStateChanged?.Invoke();
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				_chatSettings.Settings.Scripts.DeepChanged -= ScriptsSettings_DeepChanged;
				ApplicationSettingsAccessor.ApplicationSettings.InheritedChatSettings.Scripts.DeepChanged -= ScriptsSettings_DeepChanged;
				_addonHook.OnReloaded -= AddonHook_OnReloaded;
			}
		}

		public IEnumerable<LuaScriptInfo> GetEffectiveScripts()
		{
			return _scriptsetCollector.GetAddonsForChat().OrderBy(s => s.IsNative ? 0 : 1);
		}
	}
}