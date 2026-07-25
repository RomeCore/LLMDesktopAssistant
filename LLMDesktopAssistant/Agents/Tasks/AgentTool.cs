using System.Text.Json.Nodes;
using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.Agents.Tasks
{
	public abstract class AgentTool
	{
		/// <summary>
		/// The name of the tool. This should be a unique identifier for the tool within the agent's context.
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// A brief description of what the tool does. This should be helpful for agents to understand how to use the tool.
		/// </summary>
		public abstract string Description { get; }

		/// <summary>
		/// A JSON schema describing the expected arguments for the tool.
		/// </summary>
		public abstract JsonObject ArgumentSchema { get; }

		/// <summary>
		/// The level of approval required for this tool to be executed.
		/// </summary>
		public ToolApprovalLevel ApprovalLevel { get; init; } = ToolApprovalLevel.PolicyBased;

		/// <summary>
		/// Preview-executes the tool with the provided arguments to determine it's behaviour or interrupt the execution.
		/// </summary>
		/// <param name="arguments">The arguments to pass to the tool. This can be null if the tool does not require any arguments.</param>
		/// <param name="cancellationToken">A cancellation token to allow for graceful termination of the operation.</param>
		/// <returns>A task that represents the asynchronous operation and returns a preview result.</returns>
		public abstract Task<AgentToolCallPreResult> PreExecuteAsync(JsonNode? arguments, CancellationToken cancellationToken = default);

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
