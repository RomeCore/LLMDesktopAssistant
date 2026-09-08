namespace LLMDesktopAssistant.Controls.Text;

/// <summary>
/// The result of <see cref="IHighlightTransformProvider.Transform"/>: an optional rendered text
/// and/or an optional list of highlight spans. Spans are in character coordinates of the raw text,
/// ghost/rendered suffix excluded. At least one member should be non-null.
/// </summary>
/// <param name="RenderedText">
/// The optional text used for rendering instead of the raw text. Safe transforms only: isometric
/// (1:1, same length) or suffix-only (append at the end), otherwise the caret/selection mapping
/// gets corrupted.
/// </param>
/// <param name="HighlightSpans">The optional highlight spans of the raw text.</param>
public readonly record struct HighlightTransformResult(string? RenderedText,
	IReadOnlyList<TextHighlightSpan>? HighlightSpans)
{
	/// <summary>
	/// Creates a result with a rendered text only (highlighting disabled).
	/// </summary>
	public HighlightTransformResult(string renderedText) : this(renderedText, null)
	{
	}

	/// <summary>
	/// Creates a result with highlight spans only (text rendering untouched).
	/// </summary>
	public HighlightTransformResult(IReadOnlyList<TextHighlightSpan> highlightSpans) : this(null,
		highlightSpans)
	{
	}
}
