using LLMDesktopAssistant.Addons.Loading;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Scripting.Lua
{
	[Service(typeof(IAddonFileLocator<LuaScriptInfo>))]
	public class LuaScriptFileLocator : AddonFileLocatorBase<LuaScriptInfo>
	{
		protected override string[] Extensions => [ ".alua", ".lua" ];

		protected override string[] Folders => [ Path.Combine("scripts", "lua") ];

		protected override bool AllowShortFormat => true;

		protected override bool UseNameDeduplication => false;

		protected override string? FullFormatName => null; // Disallow full format
	}
}
