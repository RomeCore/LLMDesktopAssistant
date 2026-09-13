using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Scripting.Lua.API;

namespace LLMDesktopAssistant.Scripting.Lua
{
	[ChatService(typeof(IAddonSetCollector<LuaScriptInfo>))]
	public class LuaScriptsetCollector(
		IChatSettingsService chatSettings,
		IEnumerable<LuaApiBase> nativeApis,
		IServiceProvider services
	) : AddonSetCollectorBase<LuaScriptInfo, LuaScriptChange>(services)
	{
		// File-based scripts CANNOT override the behavior of native APIs
		protected override bool AdditionalGoingFirst => false;

		protected override IEnumerable<LuaScriptInfo> GetAdditionalAddons()
		{
			return nativeApis.Select(a =>
			{
				return new LuaScriptInfo
				{
					Enabled = true,
					IsNative = true,
					Name = "$native+" + a.GetType().Name,
					Namespace = a.Namespace ?? string.Empty,
					Manuals = a.Manuals,
					Loader = (globals, ns, lua) =>
					{
						return a.Populate(globals, ns!, lua);
					}
				};
			});
		}

		public override IEnumerable<LuaScriptInfo> GetAddonsForChat()
		{
			var settings = chatSettings.Settings.Scripts;
			var scriptset = settings.GetEffectiveScriptset();
			return GetAddonsWithChanges(scriptset.ScriptChanges, enabledByDefault: scriptset.EnabledByDefault, hiddenByDefault: false, agent: null);
		}
	}
}
