using LLMDesktopAssistant.LLM.Domain;
using LLTSharp;
using LLTSharp.Locale;
using LLTSharp.Metadata;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;
using System.Globalization;

namespace LLMDesktopAssistant.LLM.Services
{
	public class PromptChatBuilder(
		Chat chat,
		TemplateLibrary templates
		) : IPromptChatBuilder
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

		private LanguageMetadata GetCurrentLanguageMetadata()
		{
			return new LanguageMetadata(new LanguageCode(CultureInfo.CurrentCulture));
		}

		private string BuildSystemPrompt(string? summaryOfPrevMessages)
		{
			var language = GetCurrentLanguageMetadata();
			var template = templates.TryRetrieveBestWithFallback("system_prompt", language) as ITextTemplate;
			var context = new
			{
				instructions = chat.Settings.SystemInstructions ?? "You are a helpful assistant.",
				personality = chat.Settings.Personality,
				summary = string.IsNullOrWhiteSpace(summaryOfPrevMessages) ? null : summaryOfPrevMessages
			};
			return template!.Render(context);
		}

		private string UpdateUserLLMProvidedContent(Domain.UserMessage message)
		{
			var language = GetCurrentLanguageMetadata();
			var template = templates.TryRetrieveBestWithFallback("user_message_prompt", language) as ITextTemplate;
			var context = new
			{
				time_sent = message.CreatedAt.ToString(),
				attachments = message.Attachments,
				content = message.Content
			};
			var result = template!.Render(context);
			return result;
		}

		public IEnumerable<IMessage> ConvertMessage(ChatMessage message)
		{
			if (message is Domain.UserMessage userMessage)
			{
				userMessage.LLMProvidedContent ??= UpdateUserLLMProvidedContent(userMessage);
				var resultMessage = new RCLargeLanguageModels.Messages.UserMessage(
					userMessage.LLMProvidedContent
				);
				return [resultMessage];
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

		public IEnumerable<IMessage> Build()
		{
			List<IMessage> messages = [];

			string? summaryOfPrevMessages = null;
			bool encounteredUserMessage = false;

			for (int i = chat.Messages.Count - 1; i >= 0; i--)
			{
				var message = chat.Messages[i].Message;

				if (message is Domain.UserMessage userMessage)
				{
					encounteredUserMessage = true;
					if (summaryOfPrevMessages != null)
					{
						messages.InsertRange(0, ConvertMessage(message));
						break;
					}
				}

				if (!string.IsNullOrWhiteSpace(message.SummaryOfPrevMessages))
				{
					summaryOfPrevMessages = message.SummaryOfPrevMessages;
					if (encounteredUserMessage)
						break;
				}
				if (summaryOfPrevMessages == null)
				{
					messages.InsertRange(0, ConvertMessage(message));
				}
			}

			string systemPrompt = BuildSystemPrompt(summaryOfPrevMessages);
			messages.Insert(0, new SystemMessage(systemPrompt));

			return messages;
		}
	}
}