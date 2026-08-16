namespace LLMDesktopAssistant.Tools.Specifiers;

/// <summary>
/// The mode in which multiple matching specifier verdicts are aggregated.
/// </summary>
public enum SpecifierAggregationMode
{
	/// <summary>
	/// Specifiers are evaluated in order; the last matching specifier wins.
	/// </summary>
	Sequential,

	/// <summary>
	/// Specifiers are aggregated by strictness: Deny &gt; Ask &gt; Allow, regardless of the order.
	/// </summary>
	Prioritized
}
