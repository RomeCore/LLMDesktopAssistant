namespace LLMDesktopAssistant.Tools.Specifiers;

/// <summary>
/// The decision applied when a tool specifier pattern matches the tool arguments.
/// </summary>
public enum SpecifierDecision
{
	/// <summary>
	/// The tool execution is approved without user confirmation.
	/// Requires a full match of the specifier pattern.
	/// </summary>
	Allow,

	/// <summary>
	/// The tool execution requires user confirmation.
	/// A partial match is enough.
	/// </summary>
	Ask,

	/// <summary>
	/// The tool execution is disallowed.
	/// A partial match is enough.
	/// </summary>
	Deny
}
