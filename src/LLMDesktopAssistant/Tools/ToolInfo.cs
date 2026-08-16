using System.Text.Json.Nodes;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Tools.Specifiers;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// The class that provides information about a tool.
	/// </summary>
	public class ToolInfo
	{
		/// <summary>
		/// The name of the tool. This is a required property.
		/// </summary>
		public required string Name { get; init; }

		/// <summary>
		/// The aliases for the tool. These are alternative names that can be used to invoke the tool.
		/// </summary>
		public ImmutableList<string> Aliases { get; init; } = [];

		/// <summary>
		/// A function that returns the description of the tool. This is useful for dynamic descriptions based on runtime conditions.
		/// </summary>
		public required Func<string> DescriptionGetter { get; init; } = () => "";

		/// <summary>
		/// A JSON object that defines the schema of the arguments for the tool.
		/// </summary>
		public required JsonObject ArgumentSchema { get; init; }

		/// <summary>
		/// A JSON object that defines the schema of the structured output for the tool.
		/// Can be null if tool does not produces structured output.
		/// </summary>
		public JsonObject? OutputSchema { get; init; }

		/// <summary>
		/// Gets a <see cref="FunctionTool"/> instance that represents this tool.
		/// Used for API registration purposes.
		/// </summary>
		public FunctionTool Tool => new(Name, DescriptionGetter(), ArgumentSchema,
			(_, _) => throw new NotSupportedException("This function tool is not meant to be executed directly. Use Executor instead."));

		/// <summary>
		/// A <see cref="FunctionTool"/> instance that represents this tool.
		/// Can be executed also (instead of <see cref="Tool"/>), usable for RCLLM agentic API.
		/// </summary>
		public FunctionTool GetExecutableTool(ToolExecutionContext ctx) => new(Name, DescriptionGetter(), ArgumentSchema,
			async (args, ct) =>
			{
				try
				{
					var result = await Executor.Invoke(args, ctx, ct);
					var success = await result.Completion;
					var content = result.ResultContent;
					if (string.IsNullOrEmpty(content))
						content = "Tool did not returned any result.";
					return new ToolResult(success ? ToolResultStatus.Success : ToolResultStatus.Error, content);
				}
				catch (Exception ex)
				{
					return new ToolResult(ToolResultStatus.Error, $"Error occured while executing tool: " + ex.Message);
				}
			});

		/// <summary>
		/// A streaming arguments analyser function for the tool. This function is executed every update for streaming tool arguments.
		/// </summary>
		public Func<JsonNode?, ToolExecutionContext, StreamingToolArgumentsAnalysisResult>? StreamingArgumentsAnalyser { get; init; }

		/// <summary>
		/// Gets or sets the default expected behaviour of the tool.
		/// </summary>
		public ToolBehaviour DefaultExpectedBehaviour { get; init; }

		/// <summary>
		/// A pre-execution function for the tool. This function is responsible for performing any necessary checks or preparations before executing the tool.
		/// </summary>
		public Func<JsonNode?, ToolExecutionContext, CancellationToken, Task<PreviewToolExecutionResult>>? PreviewExecutor { get; init; }

		/// <summary>
		/// A list of all possible specifier parameters that can be used with the tool.
		/// </summary>
		public ImmutableList<string> SpecifierParameters { get; init; } = [];

		/// <summary>
		/// A function that analyzes a specifier and determines if it matches the tool's arguments.
		/// </summary>
		public Func<Specifier, JsonNode?, ToolExecutionContext, SpecifierMatchResult>? SpecifierAnalyzer { get; init; }

		/// <summary>
		/// Specifies whether the tool overrides the standard HITL pipeline with its own policy decisions.
		/// </summary>
		public ToolPolicyDecision DefaultSelfHandledDecisions { get; init; }

		/// <summary>
		/// The executor function for the tool. This function is responsible for executing the tool with the provided arguments and context.
		/// </summary>
		public required Func<JsonNode?, ToolExecutionContext, CancellationToken, Task<ReactiveToolResult>> Executor { get; init; }

		/// <summary>
		/// A synchronization group for the tool. Used for executing multiple tools in same group one-by-one. This is useful for tools that should not run at the same time (e.g. file editing tools).
		/// </summary>
		public string? SynchronizationGroup { get; init; }

		/// <summary>
		/// The localization key of the user-friendly display name of the tool.
		/// If not set, the tool's name is used as the display name.
		/// </summary>
		public LocaleKeyBase? TitleKey { get; init; }

		/// <summary>
		/// The localization key of the tool description shown in the UI.
		/// If not set, the raw description from <see cref="DescriptionGetter"/> is used.
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
		/// A value indicating whether the tool is enabled. Defaults to true.
		/// </summary>
		public bool Enabled { get; init; } = true;

		/// <summary>
		/// A value indicating whether the tool is fixed and cannot be disabled (but approval level can be modified).
		/// Usually the fixed tools are context-based, <c>skill-load</c> for example (only enabled when skills present in the system).
		/// </summary>
		public bool IsFixed { get; init; } = false;

		/// <summary>
		/// A value indicating whether the tool requires user confirmation before execution.
		/// </summary>
		public ToolApprovalLevel ApprovalLevel { get; init; } = ToolApprovalLevel.PolicyBased;

		/// <summary>
		/// The individual policy mask that overrides the agent's policy for this tool.
		/// <see cref="ToolIndividualPolicyMask.DisallowedBehaviours"/> always disallow the tool,
		/// <see cref="ToolIndividualPolicyMask.AutoApproveBehaviours"/> always approve it.
		/// Applied only for policy-based approval levels.
		/// </summary>
		public ToolIndividualPolicyMask? PolicyMask { get; init; }

		/// <summary>
		/// The specifier behaviour union mode of the tool.
		/// Null indicates that the default mode (<see cref="SpecifierBehaviourUnionMode.CombineSoft"/>) is used.
		/// </summary>
		public SpecifierBehaviourUnionMode? SpecifierUnionMode { get; init; }

		/// <summary>
		/// The specifier aggregation mode of the tool.
		/// </summary>
		public SpecifierAggregationMode SpecifierAggregationMode { get; init; } = SpecifierAggregationMode.Sequential;

		/// <summary>
		/// The specifier rules of the tool.
		/// </summary>
		public ImmutableList<ToolSpecifierRule> Specifiers { get; init; } = [];

		/// <summary>
		/// List of tools that was overriden during deduplication by name.
		/// </summary>
		public ImmutableList<ToolInfo> Overrides { get; init; } = [];

		/// <summary>
		/// Creates a new instance of the <see cref="ToolInfo"/> class with the specified executor and initialization information.
		/// </summary>
		/// <param name="info">The initialization information for the tool. This includes various properties such as name, description, and category.</param>
		/// <returns>The newly created <see cref="ToolInfo"/> instance.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the description getter is not provided in the initialization information.</exception>
		public static ToolInfo Create(ToolInitializationInfo info)
		{
			ToolName.EnsureValid(info.Name);

			if (info.DescriptionGetter == null)
				throw new InvalidOperationException($"Description of the {nameof(ToolInitializationInfo)} must be set before tool creation.");

			var (argSchema, _executor, parameters) = ToolExecutorCreator.Create(info.Executor);
			var _streamingAnalyzer = info.StreamingAnalyzer != null ?
				StreamingToolArgumentAnalyzerCreator.Create(info.StreamingAnalyzer, parameters) : null;
			var _previewExecutor = info.PreviewExecutor != null ?
				PreviewToolExecutorCreator.Create(info.PreviewExecutor, parameters) : null;
			var _specifierAnalyzer = info.SpecifierAnalyzer != null ?
				SpecifierToolAnalyzerCreator.Create(info.SpecifierAnalyzer, parameters) : null;

			info.ModifyArgumentSchema?.Invoke(argSchema);

			return new ToolInfo
			{
				Name = info.Name,
				Aliases = info.Aliases,
				DescriptionGetter = info.DescriptionGetter,
				ArgumentSchema = argSchema,
				OutputSchema = info.OutputSchema,
				StreamingArgumentsAnalyser = _streamingAnalyzer,
				DefaultExpectedBehaviour = info.DefaultExpectedBehaviour,
				PreviewExecutor = _previewExecutor,
				SpecifierParameters = info.SpecifierParameters,
				SpecifierAnalyzer = _specifierAnalyzer,
				DefaultSelfHandledDecisions = info.DefaultSelfHandledDecisions,
				Executor = _executor,
				SynchronizationGroup = info.SynchronizationGroup,
				TitleKey = info.TitleKey,
				DescriptionKey = info.DescriptionKey,
				CategoryKey = info.CategoryKey,
				ApprovalLevel = info.ApprovalLevel,
				Enabled = info.Enabled,
				IsFixed = info.IsFixed,
				Source = info.Source
			};
		}
	}
}