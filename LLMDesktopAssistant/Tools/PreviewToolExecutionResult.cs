using Material.Icons;

namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// Represents the result of a tool's pre-execution check.
	/// </summary>
	public class PreviewToolExecutionResult
	{
		/// <summary>
		/// The status icon to be displayed. This will be shown next to the main title (that contains tool name).
		/// </summary>
		public MaterialIconKind? StatusIcon { get; init; }

		/// <summary>
		/// The title of the status that will be shown next to the main title (that contains tool name).
		/// </summary>
		public string? StatusTitle { get; init; }

		/// <summary>
		/// The level of danger associated with the pre-execution check.
		/// </summary>
		public ToolDangerLevel DangerLevel { get; init; }
	}
}