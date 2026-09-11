using System.Text.Json.Nodes;
using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Tools.Specifiers;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// The class that provides information about a tool.
	/// </summary>
	/// <remarks>
	/// Tools used to live in two parallel worlds: plain <see cref="ToolInfo"/> produced by tool modules,
	/// and the separately managed "meta-tools" (the <c>Tools/Meta/*</c> subsystem, born in d859a35 on
	/// 2026-04-09 and buried on 2026-09-12). There is only one world now - every tool is an addon,
	/// possibly with a scripting engine behind it. See the epitaph in
	/// <c>Tools/Scripting/ScriptableToolParser.cs</c>.
	/// </remarks>
	public class ToolInfo : AddonChangedBase<ToolInfo, ToolChange>
	{
		/// <summary>
		/// A JSON object that defines the schema of the arguments for the tool.
		/// </summary>
		public JsonObject ArgumentSchema
		{
			get => field ??= new JsonObject
			{
				["type"] = "object",
				["additionalProperties"] = false
			};
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// A JSON object that defines the schema of the structured output for the tool.
		/// Can be null if tool does not produces structured output.
		/// </summary>
		public JsonObject? OutputSchema
		{
			get => field;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// Gets a <see cref="FunctionTool"/> instance that represents this tool.
		/// Used for API registration purposes.
		/// </summary>
		public FunctionTool NativeTool => new(Name, Description, ArgumentSchema,
			(_, _) => throw new NotSupportedException("This function tool is not meant to be executed directly. Use Executor instead."));

		/// <summary>
		/// A <see cref="FunctionTool"/> instance that represents this tool.
		/// Can be executed also (instead of <see cref="NativeTool"/>), usable for RCLLM agentic API.
		/// </summary>
		public FunctionTool GetExecutableTool(ToolExecutionContext ctx) => new(Name, Description, ArgumentSchema,
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
		public Func<JsonNode?, ToolExecutionContext, StreamingToolArgumentsAnalysisResult>? StreamingAnalyzer
		{
			get => field;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// Gets or sets the default expected behaviour of the tool.
		/// </summary>
		public ToolBehaviour DefaultExpectedBehaviour
		{
			get => field;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// A pre-execution function for the tool. This function is responsible for performing any necessary checks or preparations before executing the tool.
		/// </summary>
		public Func<JsonNode?, ToolExecutionContext, CancellationToken, Task<PreviewToolExecutionResult>>? PreviewExecutor
		{
			get => field;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// A list of all possible specifier parameters that can be used with the tool.
		/// </summary>
		public ImmutableList<string> SpecifierParameters
		{
			get => field;
			set => SetProperty(ref field, value);
		} = [];

		/// <summary>
		/// A function that analyzes a specifier and determines if it matches the tool's arguments.
		/// </summary>
		public Func<Specifier, JsonNode?, ToolExecutionContext, SpecifierMatchResult>? SpecifierAnalyzer
		{
			get => field;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// Specifies whether the tool overrides the standard HITL pipeline with its own policy decisions.
		/// </summary>
		public ToolPolicyDecision DefaultSelfHandledDecisions
		{
			get => field;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The executor function for the tool. This function is responsible for executing the tool with the provided arguments and context.
		/// </summary>
		public Func<JsonNode?, ToolExecutionContext, CancellationToken, Task<ReactiveToolResult>> Executor
		{
			get => field ??= (_, _, _) => Task.FromResult(
				ReactiveToolResult.CreateError($"Tool '{Name}' has no executor (it is probably a broken addon file)."));
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// A synchronization group for the tool. Used for executing multiple tools in same group one-by-one. This is useful for tools that should not run at the same time (e.g. file editing tools).
		/// </summary>
		public string? SynchronizationGroup
		{
			get => field;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The source of the tool. Defaults to "native".
		/// </summary>
		public ToolSource ToolSource
		{
			get => field;
			set => SetProperty(ref field, value);
		} = ToolSource.Native;

		/// <summary>
		/// A value indicating whether the tool requires user confirmation before execution. By default, uses agent's default approval level.
		/// </summary>
		public ToolApprovalLevel? ApprovalLevel
		{
			get => field;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The individual policy mask that overrides the agent's policy for this tool.
		/// <see cref="ToolPolicyMask.DisallowedBehaviours"/> always disallow the tool,
		/// <see cref="ToolPolicyMask.AutoApproveBehaviours"/> always approve it.
		/// Applied only for policy-based approval levels.
		/// </summary>
		public ToolPolicyMask? PolicyMask
		{
			get => field;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The specifier behaviour union mode of the tool.
		/// Null indicates that the default mode (<see cref="SpecifierBehaviourUnionMode.CombineSoft"/>) is used.
		/// </summary>
		public SpecifierBehaviourUnionMode? SpecifierUnionMode
		{
			get => field;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// The specifier aggregation mode of the tool.
		/// </summary>
		public SpecifierAggregationMode SpecifierAggregationMode
		{
			get => field;
			set => SetProperty(ref field, value);
		} = SpecifierAggregationMode.Sequential;

		/// <summary>
		/// The specifier rules of the tool.
		/// </summary>
		public ImmutableList<ToolSpecifierRule> Specifiers
		{
			get => field;
			set => SetProperty(ref field, value);
		} = [];

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

			var tool = info.Clone();

			tool.ArgumentSchema = argSchema;
			tool.Executor = _executor;
			tool.StreamingAnalyzer = _streamingAnalyzer;
			tool.PreviewExecutor = _previewExecutor;
			tool.SpecifierAnalyzer = _specifierAnalyzer;

			tool.Freeze();

			return tool;
		}
	}
}