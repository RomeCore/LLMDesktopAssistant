using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Core.LLM.Data;
using LLMDesktopAssistant.Core.LLM.Domain;
using LLMDesktopAssistant.Core.LLM.Services;
using LLMDesktopAssistant.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;
using RCLargeLanguageModels.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LLMDesktopAssistant.Avalonia.LLM.Messages
{
	[ViewModelFor(typeof(AssistantMessageView))]
	public class AssistantMessageViewModel : MessageViewModelBase
	{
		private readonly AssistantMessage _assistantMessage;

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
						assistantMessage.ToolCalls.Select(t => new ToolCallViewModel(t)))
				};
			}
			Error = assistantMessage.Error;

			IsCompleted = assistantMessage.IsCompleted;
			if (!assistantMessage.IsCompleted)
			{
				void OnMessagePropertyChanged(object? s, PropertyChangedEventArgs e)
				{
					InvokeUI(() =>
					{
						if (ReasoningPart == null && !string.IsNullOrEmpty(assistantMessage.ReasoningContent))
						{
							ReasoningPart = new AssistantMessageReasoningPartViewModel(assistantMessage);
						}
						if (TextPart == null && !string.IsNullOrEmpty(assistantMessage.Content))
						{
							TextPart = new AssistantMessageTextPartViewModel(assistantMessage);
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
								ToolPart.ToolCalls.Add(new ToolCallViewModel(toolCall));
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