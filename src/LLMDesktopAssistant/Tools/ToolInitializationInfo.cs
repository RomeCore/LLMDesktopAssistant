using System.Text.Json.Nodes;
using LLMDesktopAssistant.Localization;

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
		/// The action to modify arguments before they are passed to the <see cref="ToolInfo"/>.
		/// </summary>
		public Action<JsonObject>? ModifyArgumentSchema { get; init; } = null;

		/// <summary>
		/// Gets or sets the default expected behaviour of the tool.
		/// </summary>
		public ToolBehaviour DefaultExpectedBehaviour { get; init; }

		/// <summary>
		/// Specifies whether the tool overrides the standard HITL pipeline with its own policy decisions.
		/// </summary>
		public ToolPolicyDecision DefaultSelfHandledDecisions { get; init; }

		/// <summary>
		/// Gets or sets a synchronization group for the tool. Used for executing multiple tools in same group one-by-one. This is useful for tools that should not run at the same time (e.g. file editing tools).
		/// </summary>
		public string? SynchronizationGroup { get; init; }

		/// <summary>
		/// Gets or sets a JSON object that defines the schema of the structured output for the tool.
		/// Can be null if tool does not produces structured output.
		/// </summary>
		public JsonObject? OutputSchema { get; init; }

		/// <summary>
		/// Gets or sets the localization key of the user-friendly display name of the tool.
		/// If not set, the tool's name is used as the display name.
		/// </summary>
		public LocaleKeyBase? TitleKey { get; init; }

		/// <summary>
		/// Gets or sets the localization key of the tool description shown in the UI.
		/// If not set, the raw description from <see cref="ToolInfo.DescriptionGetter"/> is used.
		/// </summary>
		public LocaleKeyBase? DescriptionKey { get; init; }

		/// <summary>
		/// Gets or sets the localization key of the tool category shown in the UI.
		/// If not set, an unknown category placeholder is used.
		/// </summary>
		public LocaleKeyBase? CategoryKey { get; init; }

		/// <summary>
		/// Gets or sets the source of the tool. Defaults to "native".
		/// </summary>
		public ToolSource Source { get; init; } = ToolSource.Native;

		/// <summary>
		/// Gets or sets a value indicating whether the tool is enabled. Defaults to true.
		/// </summary>
		public bool Enabled { get; init; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether the tool is fixed and cannot be disabled (but approval level can be modified).
		/// Usually the fixed tools are context-based, <c>skill-load</c> for example (only enabled when skills present in the system).
		/// </summary>
		public bool IsFixed { get; init; } = false;

		/// <summary>
		/// Gets or sets a value indicating whether the tool requires user confirmation before execution.
		/// </summary>
		public ToolApprovalLevel ApprovalLevel { get; init; } = ToolApprovalLevel.PolicyBased;
	}
}