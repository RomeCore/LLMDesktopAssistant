using System.Text.Json.Nodes;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Scripting;
using LLTSharp;
using ModelContextProtocol.Protocol;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// The context in which a tool is executed.
	/// This used to provide additional information about the execution environment of a tool.
	/// </summary>
	public class ToolExecutionContext
	{
		/// <summary>
		/// The chat instance where tool is being executed.
		/// </summary>
		public required Chat Chat { get; init; }

		/// <summary>
		/// The assistant message that contains tool call that being executed.
		/// </summary>
		public required AssistantMessage Message { get; init; }

		/// <summary>
		/// The tool call that is being executed.
		/// </summary>
		public required ToolCall Call { get; init; }

		/// <summary>
		/// Information about the tool that is being executed.
		/// </summary>
		public required ToolInfo Info { get; init; }

		/// <summary>
		/// Whether the tool is running in a user interface.
		/// </summary>
		public required bool RunningInUI { get; init; }

		/// <summary>
		/// The decision made by the tool execution pipeline.
		/// For non-UI contexts, this will be <see cref="ToolPolicyDecision.None"/> at most times.
		/// For UI contexts and without a specific self-handled decisions 
		/// (see <see cref="ToolInfo.DefaultSelfHandledDecisions"/> and <see cref="PreviewToolExecutionResult.SelfHandledDecisions"/>)
		/// this will be <see cref="ToolPolicyDecision.Approve"/>.
		/// For UI contexts with a specific self-handled decisions this will be the decision made by the tool execution pipeline.
		/// </summary>
		public required ToolPolicyDecision PolicyDecision { get; init; } = ToolPolicyDecision.None;

		/// <summary>
		/// The shared context that can be used to pass data between streaming, preview and main execution calls.
		/// </summary>
		public object? SharedContext { get; set; }

		/// <summary>
		/// Creates a dummy tool execution context. Useful when the original execution context is not available.
		/// </summary>
		public static ToolExecutionContext CreateDummy(ToolInfo tool, JsonNode? args, Chat? chat)
		{
			var ct = new CompletionToken();
			var toolCall = new ToolCall
			{
				ToolName = tool.Name,
				Title = tool.DisplayName,
				CompletionToken = ct,
				Id = ToolCallId.Generate(),
				Arguments = args?.ToJsonString() ?? "{}"
			};
			var message = new AssistantMessage
			{
				CreatedAt = DateTime.Now,
				AgentStageId = Guid.Empty,
				SenderAgentId = Guid.Empty,
				CompletionToken = ct
			};
			message.ToolCalls.Add(toolCall);

			return new ToolExecutionContext
			{
				Chat = chat ?? new Chat(new ServiceCollection().BuildServiceProvider()),
				Call = toolCall,
				Message = message,
				Info = tool,
				RunningInUI = false,
				PolicyDecision = ToolPolicyDecision.None
			};
		}
	}
}