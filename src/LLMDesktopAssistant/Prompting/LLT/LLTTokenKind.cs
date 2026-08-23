namespace LLMDesktopAssistant.Prompting.LLT;

/// <summary>
/// Defines the semantic kinds of LLT text segments used for syntax highlighting.
/// </summary>
public enum LLTTokenKind
{
	/// <summary>
	/// Plain template text that is rendered as-is; not colorized.
	/// </summary>
	PlainText,

	/// <summary>
	/// A language keyword, such as <c>template</c>, <c>if</c> or <c>foreach</c>.
	/// </summary>
	Keyword,

	/// <summary>
	/// An identifier: a template name, variable, field or method name.
	/// </summary>
	Identifier,

	/// <summary>
	/// A numeric literal.
	/// </summary>
	Number,

	/// <summary>
	/// A string literal inside an expression.
	/// </summary>
	String,

	/// <summary>
	/// An operator or punctuation symbol, such as <c>&gt;</c>, <c>+</c> or <c>.</c>.
	/// </summary>
	Operator,

	/// <summary>
	/// A control character or delimiter, such as <c>@</c>, <c>{</c> or <c>(</c>.
	/// </summary>
	Control,

	/// <summary>
	/// A comment.
	/// </summary>
	Comment,

	/// <summary>
	/// A fragment that failed to parse; rendered as an error.
	/// </summary>
	Error,
}
