namespace LLMDesktopAssistant.Prompting.LLT;

/// <summary>
/// Represents a parse error with its source position.
/// </summary>
/// <param name="Position">The offset of the error in the source text.</param>
/// <param name="Line">The 1-based line number of the error.</param>
/// <param name="Column">The 1-based column number of the error.</param>
/// <param name="Message">The error message.</param>
public readonly record struct LLTParseError(int Position, int Line, int Column, string Message)
{
	/// <summary>
	/// Gets a human-readable representation of the error with its position.
	/// </summary>
	public string Display => $"L{Line}:C{Column}: {Message}";
}
