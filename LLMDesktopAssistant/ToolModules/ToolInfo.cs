using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.ToolModules
{
	/// <summary>
	/// The class that provides information about a tool.
	/// </summary>
	public class ToolInfo
	{
		/// <summary>
		/// Gets or sets the tool.
		/// </summary>
		public required ITool Tool { get; init; }

		/// <summary>
		/// Gets or sets a value indicating whether the tool requires user confirmation before execution.
		/// </summary>
		public bool AskForConfirmation { get; set; } = false;
	}
}