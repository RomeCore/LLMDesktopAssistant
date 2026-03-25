using LLMDesktopAssistant.LLM.Domain;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;
using XamlMath.Utils;

namespace LLMDesktopAssistant.LLM.Services
{
	public class MessageConverter : IMessageConverter
	{
		private ToolResultStatus ConvertToolStatus(ToolStatus status)
		{
			return status switch
			{
				ToolStatus.NotExecuted => ToolResultStatus.NoResult,
				ToolStatus.WaitingForApproval => ToolResultStatus.NoResult,
				ToolStatus.Executing => ToolResultStatus.NoResult,
				ToolStatus.Success => ToolResultStatus.Success,
				ToolStatus.Error => ToolResultStatus.Error,
				ToolStatus.Cancelled => ToolResultStatus.Cancelled,
				_ => throw new ArgumentOutOfRangeException(nameof(status), status, "Invalid status."),
			};
		}

		private AssistantMessageStatus ConvertAssistantMessageStatus(CompletionState state)
		{
			return state switch
			{
				CompletionState.Incomplete => AssistantMessageStatus.Pending,
				CompletionState.Success => AssistantMessageStatus.Success,
				CompletionState.Cancelled => AssistantMessageStatus.Cancelled,
				CompletionState.Failed => AssistantMessageStatus.Error,
				_ => throw new ArgumentOutOfRangeException(nameof(state), state, "Invalid state.")
			};
		}

		public IEnumerable<IMessage> Convert(ChatMessage message)
		{
			if (message is Domain.UserMessage userMessage)
			{
				return [ new RCLargeLanguageModels.Messages.UserMessage(userMessage.Content) ];
			}
			else if (message is Domain.AssistantMessage assistantMessage)
			{
				List<IToolCall> toolCalls = [];
				List<IMessage> messages = [];

				foreach (var toolCall in assistantMessage.ToolCalls)
				{
					toolCalls.Add(new FunctionToolCall(toolCall.Id, toolCall.ToolName, toolCall.Arguments));
					var status = ConvertToolStatus(toolCall.Status);
					var toolResult = new ToolResult(status, toolCall.ResultContent ?? "Tool did returned no result.");
					messages.Add(new ToolMessage(toolResult, toolCall.Id, toolCall.ToolName));
				}

				var result = new RCLargeLanguageModels.Messages.AssistantMessage(assistantMessage.Content,
					assistantMessage.ReasoningContent, toolCalls: toolCalls);
				messages.Insert(0, result);

				return messages;
			}
			else
			{
				throw new InvalidOperationException($"Unsupported message type: {message.GetType()}.");
			}
		}
	}
}