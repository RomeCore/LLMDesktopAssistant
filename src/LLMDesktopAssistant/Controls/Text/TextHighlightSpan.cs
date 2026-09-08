using Avalonia.Media;

namespace LLMDesktopAssistant.Controls.Text;

/// <summary>
/// A text highlight range (in characters, coordinates of the original text, ghost not included).
/// </summary>
public readonly record struct TextHighlightSpan(int Start, int Length, IBrush Brush,
	TextDecorationCollection? Decorations = null);
