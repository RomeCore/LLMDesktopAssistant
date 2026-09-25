namespace LLMDesktopAssistant.Addons
{
	[Flags]
	public enum AddonKind
	{
		None = 0,

		All = Pack | Skill | SubAgent | Tool | Template | LuaScript,

		/// <summary>
		/// The addon pack itself, used in the invalidation methods to invalidate packs along with other addon types.
		/// </summary>
		Pack = 1 << 0,

		Skill = 1 << 1,

		SubAgent = 1 << 2,

		Tool = 1 << 3,

		PromptContext = 1 << 4,

		Template = 1 << 5,

		LuaScript = 1 << 6
	}
}
