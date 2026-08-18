namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// A record struct to hold a tool name and an optional specifier.
	/// </summary>
	/// <param name="ToolName">The name of the tool. Must not be null or empty.</param>
	/// <param name="Specifier">An optional specifier for the tool. Can be null.</param>
	public record struct ToolNameWithSpecifier(string ToolName, string? Specifier = null);
}
