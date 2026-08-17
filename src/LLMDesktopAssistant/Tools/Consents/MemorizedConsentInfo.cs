namespace LLMDesktopAssistant.Tools.Consents;

/// <summary>
/// Describes a memorized user consent decision for a tool.
/// </summary>
/// <param name="ToolName">The name of the tool.</param>
/// <param name="Approved">Whether the tool was approved (<see langword="true"/>) or denied (<see langword="false"/>).</param>
/// <param name="Notes">The user notes or the denial reason, or <see langword="null"/>.</param>
public readonly record struct MemorizedConsentInfo(string ToolName, bool Approved, string? Notes);
