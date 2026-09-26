namespace LLMDesktopAssistant.Addons
{
	[Flags]
	public enum AddonKind
	{
		None = 0,

		All = Pack | Skill | SubAgent | Tool | MemoryBlock | PromptContext | Command | Template | LuaScript,

		/// <summary>
		/// The addon pack itself, used in the invalidation methods to invalidate packs along with other addon types.
		/// </summary>
		Pack = 1 << 0,

		Skill = 1 << 1,

		SubAgent = 1 << 2,

		Tool = 1 << 3,

		// TODO: Reserved for future use
		MemoryBlock = 1 << 4,

		PromptContext = 1 << 5,

		// TODO: Reserved for future use
		Command = 1 << 6,

		Template = 1 << 7,

		LuaScript = 1 << 8
	}
}
