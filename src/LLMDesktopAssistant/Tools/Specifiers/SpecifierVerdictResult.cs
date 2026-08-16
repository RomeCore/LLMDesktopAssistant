namespace LLMDesktopAssistant.Tools.Specifiers;

/// <summary>
/// The result of the specifier layer evaluation: the aggregated verdict and the message
/// describing which specifier produced it.
/// </summary>
/// <param name="Verdict">The aggregated specifier verdict.</param>
/// <param name="Message">The message describing the matched specifier, or an empty string.</param>
public readonly record struct SpecifierVerdictResult(SpecifierVerdict Verdict, string Message);
