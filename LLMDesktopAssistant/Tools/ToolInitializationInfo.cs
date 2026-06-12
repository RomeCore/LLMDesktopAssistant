using System.Text.Json.Nodes;

namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// The class that provides initialization information for a <see cref="ToolInfo"/>.
	/// </summary>
	public class ToolInitializationInfo
	{
		/// <summary>
		/// Gets or sets the name of the tool. This is a required property.
		/// </summary>
		public required string Name { get; init; }

		/// <summary>
		/// Gets or sets the aliases for the tool. These are alternative names that can be used to invoke the tool.
		/// </summary>
		public ImmutableList<string> Aliases { get; init; } = [];

		/// <summary>
		/// Gets or sets the description of the tool.
		/// </summary>
		public string Description
		{
			init
			{
				DescriptionGetter = () => value ?? "";
			}
		}

		/// <summary>
		/// Gets or sets a function that returns the description of the tool.
		/// This is useful for dynamic descriptions based on runtime conditions.
		/// </summary>
		public Func<string> DescriptionGetter { get; init; } = null!;

		/// <summary>
		/// Gets or sets the default expected behaviour of the tool.
		/// </summary>
		public ToolBehaviour DefaultExpectedBehaviour { get; init; }

		/// <summary>
		/// Gets or sets a JSON object that defines the schema of the structured output for the tool.
		/// Can be null if tool does not produces structured output.
		/// </summary>
		public JsonObject? OutputSchema { get; init; }

		/// <summary>
		/// Gets or sets the user-friendly display name of the tool. If not set, the tool's name will be used as the display name.
		/// </summary>
		public string? DisplayName { get; set; }

		/// <summary>
		/// Gets or sets the category of the tool. Defaults to "general".
		/// </summary>
		public string Category { get; set; } = "general";

		/// <summary>
		/// Gets or sets the source of the tool. Defaults to "native".
		/// </summary>
		public ToolSource Source { get; set; } = ToolSource.Native;

		/// <summary>
		/// Gets or sets a value indicating whether the tool is enabled. Defaults to true.
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether the tool requires user confirmation before execution.
		/// </summary>
		public ToolApprovalLevel ApprovalLevel { get; init; } = ToolApprovalLevel.PolicyBased;
	}
}