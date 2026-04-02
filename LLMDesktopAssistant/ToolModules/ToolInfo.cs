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
		public required FunctionTool Tool { get; init; }

		/// <summary>
		/// Gets or sets the category of the tool. Defaults to "general".
		/// </summary>
		public string Category { get; init; } = "general";

		/// <summary>
		/// Gets or sets a value indicating whether the tool is enabled. Defaults to true.
		/// </summary>
		public bool Enabled { get; init; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether the tool requires user confirmation before execution.
		/// </summary>
		public bool AskForConfirmation { get; init; } = false;
	}
}