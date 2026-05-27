using Markdig.Syntax.Inlines;

namespace LLMDesktopAssistant.Markdown.Nodes;

/// <summary>
/// An inline Markdown object that represents a quick explanation tooltip.
/// Syntax: @[Term](definition text)
/// </summary>
public class QuickExplanation : LeafInline
{
	/// <summary>
	/// The term being explained (displayed inline).
	/// </summary>
	public string Term { get; set; } = string.Empty;

	/// <summary>
	/// The definition shown in a tooltip on hover.
	/// </summary>
	public string Definition { get; set; } = string.Empty;
}
