namespace LLMDesktopAssistant.Addons
{
	[Flags]
	public enum AddonKind
	{
		None = 0,

		All = Skill | SubAgent | Tool | Template | LuaScript,

		Skill = 1 << 0,

		SubAgent = 1 << 1,

		Tool = 1 << 2,

		Template = 1 << 3,

		LuaScript = 1 << 4
	}
}
