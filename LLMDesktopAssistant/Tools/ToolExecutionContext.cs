using System.Text.Json.Nodes;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Scripting;
using LLTSharp;
using ModelContextProtocol.Protocol;
using MoonSharp.Interpreter;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// The context in which a tool is executed.
	/// This used to provide additional information about the execution environment of a tool.
	/// </summary>
	[MoonSharpUserData]
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
				Info = tool
			};
		}
	}
}