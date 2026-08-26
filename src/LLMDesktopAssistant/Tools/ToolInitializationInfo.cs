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
		/// The name of the tool. This is a required property.
		/// </summary>
		public required string Name { get; init; }

		/// <summary>
		/// The executor delegate that will be invoked when the tool is executed. This is a required property.
		/// </summary>
		public required Delegate Executor { get; init; }

		/// <summary>
		/// The streaming analyzer delegate that will be invoked on every update of tool call arguments.
		/// </summary>
		public Delegate? StreamingAnalyzer { get; init; }

		/// <summary>
		/// The preview executor delegate that will be invoked before main executor and specifier analyzer.
		/// Used for determine behaviour of the tool and short-circuiting when arguments are not valid semantically
		/// (e.g. file for deletion not exists).
		/// </summary>
		public Delegate? PreviewExecutor { get; init; }

		/// <summary>
		/// The specifier analyzer delegate used to check tool call arguments against the specifier.
		/// Used for advanced policy checks (e.g. check against allowed command masks for shell execution).
		/// </summary>
		public Delegate? SpecifierAnalyzer { get; init; }

		/// <summary>
		/// The aliases for the tool. These are alternative names that can be used to invoke the tool.
		/// </summary>
		public ImmutableList<string> Aliases { get; init; } = [];

		/// <summary>
		/// The description of the tool.
		/// </summary>
		public string Description
		{
			init
			{
				DescriptionGetter = () => value ?? "";
			}
		}

		/// <summary>
		/// A function that returns the description of the tool.
		/// This is useful for dynamic descriptions based on runtime conditions.
		/// </summary>
		public Func<string> DescriptionGetter { get; init; } = null!;

		/// <summary>
		/// The action to modify arguments before they are passed to the <see cref="ToolInfo"/>.
		/// </summary>
		public Action<JsonObject>? ModifyArgumentSchema { get; init; } = null;

		/// <summary>
		/// The default expected behaviour of the tool.
		/// </summary>
		public ToolBehaviour DefaultExpectedBehaviour { get; init; }

		/// <summary>
		/// A list of all possible specifier parameters that can be used with the tool.
		/// </summary>
		public ImmutableList<string> SpecifierParameters { get; init; } = [];

		/// <summary>
		/// Specifies whether the tool overrides the standard HITL pipeline with its own policy decisions.
		/// </summary>
		public ToolPolicyDecision DefaultSelfHandledDecisions { get; init; }

		/// <summary>
		/// A synchronization group for the tool. Used for executing multiple tools in same group one-by-one. This is useful for tools that should not run at the same time (e.g. file editing tools).
		/// </summary>
		public string? SynchronizationGroup { get; init; }

		/// <summary>
		/// A JSON object that defines the schema of the structured output for the tool.
		/// Can be null if tool does not produces structured output.
		/// </summary>
		public JsonObject? OutputSchema { get; init; }

		/// <summary>
		/// The localization key of the user-friendly display name of the tool.
		/// If not set, the tool's name is used as the display name.
		/// </summary>
		public LocaleKeyBase? TitleKey { get; init; }

		/// <summary>
		/// The localization key of the tool description shown in the UI.
		/// If not set, the raw description from <see cref="ToolInfo.DescriptionGetter"/> is used.
		/// </summary>
		public LocaleKeyBase? DescriptionKey { get; init; }

		/// <summary>
		/// The localization key of the tool category shown in the UI.
		/// If not set, an unknown category placeholder is used.
		/// </summary>
		public LocaleKeyBase? CategoryKey { get; init; }

		/// <summary>
		/// The source of the tool. Defaults to "native".
		/// </summary>
		public ToolSource Source { get; init; } = ToolSource.Native;

		/// <summary>
		/// A value indicating whether the tool is enabled. Defaults to null, meaning it is not explicitly enabled or disabled.
		/// </summary>
		public bool? Enabled { get; init; } = null;

		/// <summary>
		/// A value indicating whether the tool is fixed and cannot be disabled (but approval level can be modified).
		/// Usually the fixed tools are context-based, <c>skill-load</c> for example (only enabled when skills present in the system).
		/// </summary>
		public bool IsFixed { get; init; } = false;

		/// <summary>
		/// A value indicating whether the tool requires user confirmation before execution.
		/// </summary>
		public ToolApprovalLevel? ApprovalLevel { get; init; } = null;
	}
}