using System.Text.Json.Nodes;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Tools.Specifiers;

namespace LLMDesktopAssistant.Agents.Tasks
{
	public abstract class AgentTool
	{
		/// <summary>
		/// The name of the tool. This should be a unique identifier for the tool within the agent's context.
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// The display name of the tool. This is used in user interfaces and should be human-readable.
		/// </summary>
		public abstract string DisplayName { get; }

		/// <summary>
		/// A brief description of what the tool does. This should be helpful for agents to understand how to use the tool.
		/// </summary>
		public abstract string Description { get; }

		/// <summary>
		/// A JSON schema describing the expected arguments for the tool.
		/// </summary>
		public abstract JsonObject ArgumentSchema { get; }

		/// <summary>
		/// The default expected behaviour for this tool.
		/// </summary>
		public ToolBehaviour DefaultExpectedBehaviour { get; init; } = ToolBehaviour.None;

		/// <summary>
		/// The level of approval required for this tool to be executed.
		/// </summary>
		public ToolApprovalLevel ApprovalLevel { get; init; } = ToolApprovalLevel.PolicyBased;

		/// <summary>
		/// The individual policy mask that overrides the agent's policy for this tool.
		/// Applied only for policy-based approval levels.
		/// </summary>
		public ToolPolicyMask? PolicyMask { get; init; } = null;

		/// <summary>
		/// The specifier behaviour union mode of the tool.
		/// Null indicates that the default mode is used.
		/// </summary>
		public SpecifierBehaviourUnionMode? SpecifierUnionMode { get; init; } = null;

		/// <summary>
		/// The specifier aggregation mode of the tool.
		/// </summary>
		public SpecifierAggregationMode SpecifierAggregationMode { get; init; } = SpecifierAggregationMode.Sequential;

		/// <summary>
		/// The specifier rules of the tool.
		/// </summary>
		public ImmutableList<ToolSpecifierRule> Specifiers { get; init; } = [];

		/// <summary>
		/// A list of all possible specifier parameters that can be used with the tool.
		/// </summary>
		public ImmutableList<string> SpecifierParameters { get; init; } = [];

		/// <summary>
		/// Preview-executes the tool with the provided arguments to determine it's behaviour or interrupt the execution.
		/// </summary>
		/// <param name="arguments">The arguments to pass to the tool. This can be null if the tool does not require any arguments.</param>
		/// <param name="cancellationToken">A cancellation token to allow for graceful termination of the operation.</param>
		/// <returns>A task that represents the asynchronous operation and returns a preview result.</returns>
		public virtual Task<AgentToolCallPreResult> PreExecuteAsync(JsonNode? arguments, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(new AgentToolCallPreResult
			{
				ExpectedBehaviour = DefaultExpectedBehaviour
			});
		}

		/// <summary>
		/// Analyzes a specifier against the tool arguments and returns the match result.
		/// The default implementation always returns <see cref="SpecifierMatchResult.NoMatch"/>.
		/// </summary>
		/// <param name="specifier">The parsed specifier to analyze. Cannot be <see langword="null"/>.</param>
		/// <param name="args">The tool arguments, or <see langword="null"/>.</param>
		/// <param name="context">The tool execution context. Cannot be <see langword="null"/>.</param>
		/// <returns>The match result of the specifier against the arguments.</returns>
		public virtual SpecifierMatchResult AnalyzeSpecifier(Specifier specifier, JsonNode? args, ToolExecutionContext context)
		{
			return SpecifierMatchResult.NoMatch;
		}

		/// <summary>
		/// Executes the tool with the given parameters. The method should handle any necessary logic and return a result.
		/// </summary>
		/// <param name="arguments">The arguments to pass to the tool. This can be null if the tool does not require any arguments.</param>
		/// <param name="sharedContext">A context object that taken from preview execution result.</param>
		/// <param name="cancellationToken">A cancellation token to allow for graceful termination of the operation.</param>
		/// <returns>A task that represents the asynchronous operation and returns the result of the tool execution.</returns>
		public abstract Task<AgentToolCallResult> ExecuteAsync(JsonNode? arguments, object? sharedContext, CancellationToken cancellationToken = default);
	}
}
