using Markdig.Syntax.Inlines;

namespace LLMDesktopAssistant.Markdown.Nodes;

/// <summary>
/// An inline Markdown object that represents a quick action button.
/// Syntax: [> Button text](prompt to insert)
/// </summary>
public class QuickAction : LeafInline
{
	/// <summary>
	/// The text displayed on the button.
	/// </summary>
	public string ButtonText { get; set; } = string.Empty;

	/// <summary>
	/// The prompt that will be inserted into chat input when clicked.
	/// </summary>
	public string Prompt { get; set; } = string.Empty;
}
