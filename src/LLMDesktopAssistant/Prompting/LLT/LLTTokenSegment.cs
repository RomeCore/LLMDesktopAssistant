namespace LLMDesktopAssistant.Prompting.LLT;

/// <summary>
/// Represents a classified fragment of LLT source text.
/// </summary>
/// <param name="Start">The start offset of the segment in the source text.</param>
/// <param name="Length">The length of the segment in characters.</param>
/// <param name="Kind">The semantic kind of the segment.</param>
public readonly record struct LLTTokenSegment(int Start, int Length, LLTTokenKind Kind)
{
	/// <summary>
	/// Gets the exclusive end offset of the segment.
	/// </summary>
	public int End => Start + Length;
}
