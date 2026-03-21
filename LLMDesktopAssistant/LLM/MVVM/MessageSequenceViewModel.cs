using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LLMDesktopAssistant.LLM.Data;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.Utils;
using Microsoft.Extensions.AI;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.LLM.MVVM
{
	[ViewModelFor(typeof(MessageSequenceView))]
	public class MessageSequenceViewModel : ViewModelBase
	{
		private readonly RangeObservableCollection<MessageViewModelBase> _messageViewModels = [];
		/// <summary>
		/// Collection of message view models that represent the sequence of messages.
		/// </summary>
		public ReadOnlyObservableCollection<MessageViewModelBase> MessageViewModels { get; }

		/// <summary>
		/// The conversation manager associated with this message sequence.
		/// </summary>
		public ConversationManager ConversationManager { get; }

		public MessageSequenceViewModel(ConversationManager conversationManager)
		{
			MessageViewModels = new ReadOnlyObservableCollection<MessageViewModelBase>(_messageViewModels);
			ConversationManager = conversationManager;

			_messageViewModels.AddRange(ConversationManager.Messages.Select(CreateMessageViewModel));
			ConversationManager.CollectionChanged += OnMessagesCollectionChanged;
		}

		private void OnMessagesCollectionChanged(object? s, NotifyCollectionChangedEventArgs e)
		{
			InvokeUI(() =>
			{
				switch (e.Action)
				{
					case NotifyCollectionChangedAction.Add:

						_messageViewModels.InsertRange(e.NewStartingIndex,
							e.NewItems!.Cast<ConversationMessage>().Select(CreateMessageViewModel));

						break;

					case NotifyCollectionChangedAction.Remove:

						for (int i = e.OldStartingIndex; i < e.OldStartingIndex + e.OldItems!.Count; i++)
							OnMessageViewModelRemoved(_messageViewModels[i]);
						_messageViewModels.RemoveRange(e.OldStartingIndex, e.OldItems!.Count);

						break;

					case NotifyCollectionChangedAction.Replace:

						for (int i = e.OldStartingIndex; i < e.OldStartingIndex + e.OldItems!.Count; i++)
							OnMessageViewModelRemoved(_messageViewModels[i]);
						_messageViewModels.RemoveRange(e.OldStartingIndex, e.OldItems!.Count);

						_messageViewModels.InsertRange(e.NewStartingIndex,
							e.NewItems!.Cast<ConversationMessage>().Select(CreateMessageViewModel));

						break;

					case NotifyCollectionChangedAction.Move:

						_messageViewModels.Move(e.OldStartingIndex, e.NewStartingIndex);

						break;

					case NotifyCollectionChangedAction.Reset:

						_messageViewModels.ReplaceRange(ConversationManager.Messages.Select(CreateMessageViewModel));

						break;
				}
				;
			});
		}

		private MessageViewModelBase CreateMessageViewModel(ConversationMessage conversationMessage)
		{
			var message = conversationMessage.Message.Message;

			if (message is IUserMessage)
			{
				return new UserMessageViewModel(conversationMessage);
			}
			else if (message is IAssistantMessage)
			{
				return new AssistantMessageViewModel(conversationMessage);
			}
			else
			{
				throw new InvalidOperationException("Unsupported message type.");
			}
		}

		private void OnMessageViewModelRemoved(MessageViewModelBase viewModel)
		{
			viewModel.OnRemoved();
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

			var toolCallVM = lastAssistantMessageVm.ToolPart?.ToolCalls.FirstOrDefault(t => t.ToolCallId == toolCall.Id);

			if (toolCallVM != null)
			{
				return await toolCallVM.AskUserAsync(cancellationToken);
			}

			return false;
		}
	}
}