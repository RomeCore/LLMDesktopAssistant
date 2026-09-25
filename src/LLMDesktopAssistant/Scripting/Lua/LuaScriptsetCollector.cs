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
		private readonly IReadOnlyList<LuaScriptInfo> _nativeApis = [..nativeApis.Select(a => new LuaScriptInfo
		{
			Enabled = true,
			OverrideOrder = 1,
			IsNative = true,
			Name = "%native+" + a.GetType().Name,
			Namespace = a.Namespace ?? string.Empty,
			Manuals = a.Manuals,
			Loader = (globals, ns, lua) =>
			{
				return a.Populate(globals, ns!, lua);
			}
		})];

		protected override IEnumerable<LuaScriptInfo> GetAdditionalAddons()
		{
			return _nativeApis;
		}

		public override IEnumerable<LuaScriptInfo> GetAddonsForChat()
		{
			var settings = chatSettings.Settings.Scripts;
			var scriptset = settings.GetEffectiveScriptset();
			return GetAddonsWithChanges(scriptset, agent: null);
		}
	}
}
