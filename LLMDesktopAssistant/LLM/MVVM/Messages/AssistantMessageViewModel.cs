using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.UIExtensions.MessageExtensions;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json.Nodes;

namespace LLMDesktopAssistant.LLM.Messages
{
	[ViewModelFor(typeof(AssistantMessageView))]
	public class AssistantMessageViewModel : MessageViewModelBase
	{
		private readonly AssistantMessage _assistantMessage;
		public AssistantMessage AssistantMessage => _assistantMessage;

		private bool _isCompleted;
		public bool IsCompleted
		{
			get => _isCompleted;
			private set => SetProperty(ref _isCompleted, value);
		}

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

		private string? _error;
		public string? Error
		{
			get => _error;
			set => SetProperty(ref _error, value);
		}

		public ImmutableList<MessageExtension> Extensions { get; }

		public bool ContainsToolCalls => _assistantMessage.ToolCalls.Count > 0;

		public AssistantMessageViewModel(BranchedMessage branchedMessage, ChatViewModel chatVM) : base(branchedMessage, chatVM)
		{
			if (branchedMessage.Message is not AssistantMessage assistantMessage)
				throw new InvalidOperationException("Invalid message type. Expected IAssistantMessage.");
			_assistantMessage = assistantMessage;

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
					ToolCalls = new ObservableCollection<ToolCallViewModel>(
						assistantMessage.ToolCalls.Select(t => new ToolCallViewModel(t, chatVM.Chat)))
				};
			}
			Error = assistantMessage.Error;
			Extensions = MessageExtensionManager.CreateExtensions(this, chatVM.Chat);

			IsCompleted = assistantMessage.IsCompleted;
			if (!assistantMessage.IsCompleted)
			{
				void OnMessagePropertyChanged(object? s, PropertyChangedEventArgs e)
				{
					InvokeUI(() =>
					{
						switch (e.PropertyName)
						{
							case nameof(AssistantMessage.ReasoningContent):

								if (ReasoningPart == null && !string.IsNullOrEmpty(assistantMessage.ReasoningContent))
								{
									ReasoningPart = new AssistantMessageReasoningPartViewModel(assistantMessage);
								}
								break;

							case nameof(AssistantMessage.Content):

								if (TextPart == null && !string.IsNullOrEmpty(assistantMessage.Content))
								{
									TextPart = new AssistantMessageTextPartViewModel(assistantMessage);
								}
								break;

							case nameof(AssistantMessage.PendingToolName):

								ToolPart ??= new AssistantMessageToolPartViewModel();
								if (ToolPart.ToolCalls.FirstOrDefault(t => t.Status == ToolStatus.Pending) is ToolCallViewModel pendingToolCall)
									ToolPart.ToolCalls.Remove(pendingToolCall);

								if (assistantMessage.PendingToolName != null)
								{
									var toolsetCache = chatVM.Chat.Services.GetRequiredService<IToolsetCacheService>();
									var title = toolsetCache.AvailableTools.TryGetValue(assistantMessage.PendingToolName, out var toolInfo) ?
										toolInfo.DisplayName : assistantMessage.PendingToolName;

									ToolPart.ToolCalls.Add(new ToolCallViewModel(new ToolCall
									{
										ToolName = assistantMessage.PendingToolName,
										Title = title,
										Arguments = new JsonObject(),
										CompletionToken = RCLargeLanguageModels.Tasks.CompletionToken.Success,
										Id = "",
										Status = ToolStatus.Pending
									}, chatVM.Chat));
								}

								break;
						}


						Error = assistantMessage.Error;
					});
				}

				void OnToolCallsChanged(object? sender, NotifyCollectionChangedEventArgs e)
				{
					if (e.NewItems != null)
					{
						InvokeUI(() =>
						{
							ToolPart ??= new AssistantMessageToolPartViewModel();
							foreach (ToolCall toolCall in e.NewItems)
							{
								ToolPart.ToolCalls.Add(new ToolCallViewModel(toolCall, chatVM.Chat));
							}

							RaisePropertyChanged(nameof(ContainsToolCalls));
						});
					}
				}

				assistantMessage.PropertyChanged += OnMessagePropertyChanged;
				assistantMessage.ToolCalls.CollectionChanged += OnToolCallsChanged;

				assistantMessage.CompletionToken.OnCompleted(() =>
				{
					assistantMessage.PropertyChanged -= OnMessagePropertyChanged;
					assistantMessage.ToolCalls.CollectionChanged -= OnToolCallsChanged;
					IsCompleted = true;
				});
			}
		}
	}
}