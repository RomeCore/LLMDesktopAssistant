using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Data;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.MVVM;
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

namespace LLMDesktopAssistant.LLM.MVVM
{
	[ViewModelFor(typeof(AssistantMessageView))]
	public class AssistantMessageViewModel : MessageViewModelBase
	{
		private bool _isCompleted;
		public bool IsCompleted
		{
			get => _isCompleted;
			private set => SetProperty(ref _isCompleted, value);
		}
		public Visibility CompletedVisibility => IsCompleted ? Visibility.Visible : Visibility.Collapsed;
		public Visibility NotCompletedVisibility => IsCompleted ? Visibility.Collapsed : Visibility.Visible;

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

		public AssistantMessageViewModel(BranchedMessage branchedMessage, Chat chat) : base(branchedMessage, chat)
		{
			if (branchedMessage.Message is not AssistantMessage assistantMessage)
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
					ToolCalls = new ObservableCollection<ToolCallViewModel>(
						assistantMessage.ToolCalls.Select(t => new ToolCallViewModel(t)))
				};
			}

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
					RaisePropertyChanged(nameof(CompletedVisibility));
					RaisePropertyChanged(nameof(NotCompletedVisibility));
				});
			}
		}
	}
}