using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LLMDesktopAssistant.MVVM;
using Microsoft.Extensions.AI;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.LLM.MVVM
{
	[ViewModelFor(typeof(MessageSequenceView))]
	public class MessageSequenceViewModel : ViewModelBase
	{
		private static readonly JsonSerializerOptions _toolCallSerializerOptions = new JsonSerializerOptions
		{
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
			WriteIndented = true
		};

		private readonly ObservableCollection<MessageViewModelBase> _messageViewModels = [];
		/// <summary>
		/// Collection of message view models that represent the sequence of messages.
		/// </summary>
		public ReadOnlyObservableCollection<MessageViewModelBase> MessageViewModels { get; }

		private ObservableCollection<IMessage> _messages = [];
		/// <summary>
		/// Gets or sets the collection of messages in dialogue with LLM.
		/// </summary>
		public ObservableCollection<IMessage> Messages
		{
			get => _messages;
			set => SetProperty(ref _messages, value);
		}
		
		public MessageSequenceViewModel()
		{
			MessageViewModels = new ReadOnlyObservableCollection<MessageViewModelBase>(_messageViewModels);
			Messages.CollectionChanged += OnMessagesCollectionChanged;
		}

		[OnPropertyChanged(nameof(Messages))]
		private void OnMessagesPropertyChanged(ObservableCollection<IMessage>? oldValue,
			ObservableCollection<IMessage> newValue)
		{
			if (oldValue != null)
				oldValue.CollectionChanged -= OnMessagesCollectionChanged;
			newValue.CollectionChanged += OnMessagesCollectionChanged;

			OnMessagesCollectionChanged(newValue, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		private void OnMessagesCollectionChanged(object? s, NotifyCollectionChangedEventArgs e)
		{
			bool isAddedToEnd = e.Action == NotifyCollectionChangedAction.Add &&
				(e.NewStartingIndex == -1 || (e.NewItems!.Count + e.NewStartingIndex == Messages.Count));

			var newMessages = isAddedToEnd ? e.NewItems!.Cast<IMessage>().ToList() : _messages.ToList();
			if (!isAddedToEnd)
			{
				InvokeUI(() => _messageViewModels.Clear());
			}

			foreach (var message in newMessages)
			{
				switch (message)
				{
					case IUserMessage userMessage:

						InvokeUI(() => _messageViewModels.Add(new UserMessageViewModel(userMessage)));
						break;

					case IAssistantMessage:
					case IToolMessage:

						var lastAssistantMessageVm = _messageViewModels.LastOrDefault() as AssistantMessageViewModel;
						if (lastAssistantMessageVm == null)
						{
							lastAssistantMessageVm = new AssistantMessageViewModel();
							InvokeUI(() => _messageViewModels.Add(lastAssistantMessageVm));
						}

						if (message is IAssistantMessage assistantMessage)
						{
							AssistantMessageToolPartViewModel? toolPartViewModel = null;

							InvokeUI(() =>
							{
								if (!string.IsNullOrEmpty(assistantMessage.ReasoningContent))
								{
									lastAssistantMessageVm.MessageParts
										.Add(new AssistantMessageReasoningPartViewModel(assistantMessage));
								}
								if (!string.IsNullOrEmpty(assistantMessage.Content))
								{
									lastAssistantMessageVm.MessageParts
										.Add(new AssistantMessageTextPartViewModel(assistantMessage));
								}
								if (assistantMessage.ToolCalls.Count > 0)
								{
									toolPartViewModel = new AssistantMessageToolPartViewModel
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
									lastAssistantMessageVm.MessageParts.Add(toolPartViewModel);
								}
							});

							if (message is PartialAssistantMessage partialAssistantMessage)
							{
								bool hasReasoning = false, hasContent = false;

								void PartHandler(object? s, AssistantMessageDelta e)
								{
									InvokeUI(() =>
									{
										if (!hasReasoning && !string.IsNullOrEmpty(partialAssistantMessage.ReasoningContent))
										{
											lastAssistantMessageVm.MessageParts
												.Add(new AssistantMessageReasoningPartViewModel(partialAssistantMessage));
											hasReasoning = true;
										}
										if (!hasContent && !string.IsNullOrEmpty(partialAssistantMessage.Content))
										{
											lastAssistantMessageVm.MessageParts
												.Add(new AssistantMessageTextPartViewModel(partialAssistantMessage));
											hasContent = true;
										}
										if (e.NewToolCalls != null && e.NewToolCalls.Count > 0)
										{
											if (toolPartViewModel == null)
											{
												toolPartViewModel = new AssistantMessageToolPartViewModel();
												lastAssistantMessageVm.MessageParts.Add(toolPartViewModel);
											}
											foreach (var toolCall in e.NewToolCalls)
												toolPartViewModel.ToolCalls.Add(new ToolCallViewModel
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
						}
						else if (message is IToolMessage toolMessage)
						{
							var toolCall = lastAssistantMessageVm.MessageParts
								.OfType<AssistantMessageToolPartViewModel>()
								.SelectMany(t => t.ToolCalls)
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

						break;
				}
			}
		}

		/// <summary>
		/// Asks the user to execute a specific tool call.
		/// </summary>
		/// <param name="toolCall">The tool call to execute.</param>
		/// <returns>A boolean indicating whether the user confirmed the execution of the tool call.</returns>
		public async Task<bool> AskToolExecuteAsync(IToolCall toolCall, CancellationToken cancellationToken = default)
		{
			var lastAssistantMessageVm = _messageViewModels.LastOrDefault() as AssistantMessageViewModel;
			if (lastAssistantMessageVm == null)
				return false;

			var toolCallVM = lastAssistantMessageVm.MessageParts
				.OfType<AssistantMessageToolPartViewModel>()
				.SelectMany(t => t.ToolCalls)
				.FirstOrDefault(t => t.ToolCallId == toolCall.Id);

			if (toolCallVM != null)
			{
				return await toolCallVM.AskUserAsync(cancellationToken);
			}

			return false;
		}
	}
}