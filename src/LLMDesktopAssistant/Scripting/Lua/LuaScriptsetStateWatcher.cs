using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Addons.Management;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Settings.Application;

namespace LLMDesktopAssistant.Scripting.Lua
{
	[ChatService(typeof(ILuaScriptsetStateWatcher))]
	[ChatService(typeof(IAddonManagerInvalidationHook))]
	public class LuaScriptsetStateWatcher : Disposable, ILuaScriptsetStateWatcher, IAddonManagerInvalidationHook
	{
		private readonly IAddonSetCollector<LuaScriptInfo> _scriptsetCollector;
		private readonly IChatSettingsService _chatSettings;

		public event Action? OnStateChanged;

		public LuaScriptsetStateWatcher(IAddonSetCollector<LuaScriptInfo> scriptsetCollector,
			IChatSettingsService chatSettings)
		{
			_scriptsetCollector = scriptsetCollector;
			_chatSettings = chatSettings;

			_chatSettings.Settings.Scripts.DeepChanged += ScriptsSettings_DeepChanged;
			ApplicationSettingsAccessor.ApplicationSettings.InheritedChatSettings.Scripts.DeepChanged += ScriptsSettings_DeepChanged;
		}

		private void ScriptsSettings_DeepChanged(object? sender, EventArgs e)
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
			}
		}

		public IEnumerable<LuaScriptInfo> GetEffectiveScripts()
		{
			return _scriptsetCollector.GetAddonsForChat().OrderBy(s => s.IsNative ? 0 : 1);
		}



		public void ReloadRequested(bool force)
		{
		}

		public void InvalidationRequested()
		{
		}

		public void Reloaded(bool force)
		{
			OnStateChanged?.Invoke();
		}
	}
}