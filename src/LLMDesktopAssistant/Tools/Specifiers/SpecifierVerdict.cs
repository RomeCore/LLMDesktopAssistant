namespace LLMDesktopAssistant.Tools.Specifiers;

/// <summary>
/// The verdict produced by the specifier layer of the tool HITL pipeline.
/// </summary>
public enum SpecifierVerdict
{
	/// <summary>
	/// No specifier matched the tool arguments; the standard policy decides.
	/// </summary>
	None,

	/// <summary>
	/// The tool execution is approved by a specifier.
	/// </summary>
	Allow,

	/// <summary>
	/// The tool execution requires user confirmation by a specifier.
	/// </summary>
	Ask,

	/// <summary>
	/// The tool execution is disallowed by a specifier.
	/// </summary>
	Deny
}
