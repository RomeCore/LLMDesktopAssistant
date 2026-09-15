using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// The addon type descriptor that registers the Lua script type.
	/// </summary>
	[AddonTypeDescriptor]
	public class LuaScriptAddonTypeDescriptor : IAddonTypeDescriptor
	{
		public string Type => "scripts/lua";

		public AddonKind Kind => AddonKind.LuaScript;

		public Type ClrType => typeof(LuaScriptInfo);

		public bool UseDefaultDiagnosticFactory => true;

		public bool UseDefaultSearchService => true;

		public LocaleKeyBase NameKey => Locale.GetKey("addon.type.scripts.lua.name");

		public LocaleKeyBase? DescriptionKey => Locale.GetKey("addon.type.scripts.lua.description");
	}
}
