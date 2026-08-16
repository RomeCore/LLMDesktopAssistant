namespace LLMDesktopAssistant.Tools.Specifiers
{
	public enum SpecifierMatchResult
	{
		/// <summary>
		/// The specifier did not match any part of main arguments.
		/// </summary>
		NoMatch,

		/// <summary>
		/// The specifier matched some parts (not all) of main arguments.
		/// </summary>
		PartialMatch,

		/// <summary>
		/// The specifier matched all parts of main arguments.
		/// </summary>
		FullMatch
	}
}
