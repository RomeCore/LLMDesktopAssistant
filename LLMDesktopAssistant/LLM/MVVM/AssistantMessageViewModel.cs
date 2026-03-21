using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LLMDesktopAssistant.LLM.Data;
using LLMDesktopAssistant.MVVM;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.LLM.MVVM
{
	[ViewModelFor(typeof(AssistantMessageView))]
	public class AssistantMessageViewModel : MessageViewModelBase
	{
		private static readonly JsonSerializerOptions _toolCallSerializerOptions = new JsonSerializerOptions
		{
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
			WriteIndented = true
		};

		private AssistantMessageReasoningPartViewModel? _reasoningPart;
		public AssistantMessageReasoningPartViewModel? ReasoningPart
		{
			get => _reasoningPart;
			private set => SetProperty(ref _reasoningPart, value);
		}

		private AssistantMessageTextPartViewModel? _textPart;
		public AssistantMessageTextPartViewModel? TextPart
		{
			get => _textPart;
			private set => SetProperty(ref _textPart, value);
		}

		private AssistantMessageToolPartViewModel? _toolPart;
		public AssistantMessageToolPartViewModel? ToolPart
		{
			get => _toolPart;
			private set => SetProperty(ref _toolPart, value);
		}

		public AssistantMessageViewModel(ConversationMessage conversationMessage)
		{
			if (conversationMessage.Message.Message is not IAssistantMessage assistantMessage)
				throw new InvalidOperationException("Invalid message type. Expected IAssistantMessage.");

			if (!string.IsNullOrEmpty(assistantMessage.ReasoningContent))
			{
				ReasoningPart ??= new AssistantMessageReasoningPartViewModel(assistantMessage);
			}
			if (!string.IsNullOrEmpty(assistantMessage.Content))
			{
				TextPart ??= new AssistantMessageTextPartViewModel(assistantMessage);
			}
			if (assistantMessage.ToolCalls.Count > 0)
			{
				ToolPart ??= new AssistantMessageToolPartViewModel
				{
					ToolCalls =
						new ObservableCollection<ToolCallViewModel>(
							assistantMessage.ToolCalls.Select(t => new ToolCallViewModel
							{
								Status = ToolCallStatus.InProgress,
								ToolName = t.ToolName,
								ToolCallId = t.Id,
								Arguments = t is FunctionToolCall ftc ? ftc.Args.ToJsonString(_toolCallSerializerOptions) : string.Empty
							}))
				};
			}

			if (assistantMessage is PartialAssistantMessage partialAssistantMessage)
			{
				void PartHandler(object? s, AssistantMessageDelta e)
				{
					InvokeUI(() =>
					{
						if (ReasoningPart == null && !string.IsNullOrEmpty(partialAssistantMessage.ReasoningContent))
						{
							ReasoningPart ??= new AssistantMessageReasoningPartViewModel(partialAssistantMessage);
						}
						if (TextPart == null && !string.IsNullOrEmpty(partialAssistantMessage.Content))
						{
							TextPart ??= new AssistantMessageTextPartViewModel(partialAssistantMessage);
						}
						if (e.NewToolCalls != null && e.NewToolCalls.Count > 0)
						{
							ToolPart ??= new AssistantMessageToolPartViewModel();
							foreach (var toolCall in e.NewToolCalls)
								ToolPart.ToolCalls.Add(new ToolCallViewModel
								{
									Status = ToolCallStatus.InProgress,
									ToolName = toolCall.ToolName,
									ToolCallId = toolCall.Id,
									Arguments = toolCall is FunctionToolCall ftc ? ftc.Args.ToJsonString(_toolCallSerializerOptions) : string.Empty
								});
						}
					});
				}
				void CompletedHandler(object? s, CompletedEventArgs e)
				{
					partialAssistantMessage.PartAdded -= PartHandler;
					partialAssistantMessage.Completed -= CompletedHandler;
				}
				if (!partialAssistantMessage.CompletionToken.IsCompleted)
				{
					partialAssistantMessage.PartAdded += PartHandler;
					partialAssistantMessage.Completed += CompletedHandler;
				}
			}

			void HandleToolMessage(ExtendedMessage message)
			{
				var toolMessage = (IToolMessage)message.Message;

				ToolPart ??= new();
				var toolCall = ToolPart.ToolCalls
					.FirstOrDefault(t => t.ToolCallId == toolMessage.ToolCallId);

				if (toolCall != null)
				{
					toolCall.Status = toolMessage.Status switch
					{
						ToolResultStatus.Success => ToolCallStatus.Success,
						ToolResultStatus.Cancelled => ToolCallStatus.Cancelled,
						ToolResultStatus.Error => ToolCallStatus.Error,
						ToolResultStatus.NoResult => ToolCallStatus.NoResult,
						_ => ToolCallStatus.None,
					};
					toolCall.Result = toolMessage.Content ?? string.Empty;
				}
			}

			foreach (var toolMessage in conversationMessage.Message.ToolMessages)
				HandleToolMessage(toolMessage);

			conversationMessage.Message.ToolMessages.CollectionChanged += (s, e) =>
			{
				if (e.NewItems != null)
					foreach (ExtendedMessage toolMessage in e.NewItems)
						HandleToolMessage(toolMessage);
			};
		}
	}
}