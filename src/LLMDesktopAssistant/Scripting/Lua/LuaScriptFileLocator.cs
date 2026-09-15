using LLMDesktopAssistant.Addons.Loading;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Scripting.Lua
{
	[Service(typeof(IAddonFileLocator<LuaScriptInfo>))]
	public class LuaScriptFileLocator : AddonFileLocatorBase<LuaScriptInfo>
	{
		public override string[] Extensions => [ ".alua", ".lua" ];

		public override string[] Folders => [ Path.Combine("scripts", "lua") ];

		public override bool AllowShortFormat => true;

		public override string? FullFormatName => null; // Disallow full format

		protected override bool UseNameDeduplication => false;
	}
}
