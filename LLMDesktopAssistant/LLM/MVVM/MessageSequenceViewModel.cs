using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LLMDesktopAssistant.LLM.Data;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM
{
	[ViewModelFor(typeof(MessageSequenceView))]
	public class MessageSequenceViewModel : ViewModelBase
	{
		/// <summary>
		/// Collection of message view models that represent the sequence of messages.
		/// </summary>
		public RangeObservableCollection<MessageViewModelBase> MessageViewModels { get; }

		/// <summary>
		/// The chat instance associated with this message sequence.
		/// </summary>
		public Chat Chat { get; }

		public MessageSequenceViewModel(Chat chat)
		{
			MessageViewModels = new RangeObservableCollection<MessageViewModelBase>();
			Chat = chat;

			MessageViewModels.AddRange(Chat.Messages.Select(CreateMessageViewModel));
			Chat.Messages.CollectionChanged += OnMessagesCollectionChanged;
		}

		private void OnMessagesCollectionChanged(object? s, NotifyCollectionChangedEventArgs e)
		{
			InvokeUI(() =>
			{
				switch (e.Action)
				{
					case NotifyCollectionChangedAction.Add:

						MessageViewModels.InsertRange(e.NewStartingIndex,
							e.NewItems!.Cast<BranchedMessage>().Select(CreateMessageViewModel));

						break;

					case NotifyCollectionChangedAction.Remove:

						for (int i = e.OldStartingIndex; i < e.OldStartingIndex + e.OldItems!.Count; i++)
							OnMessageViewModelRemoved(MessageViewModels[i]);
						MessageViewModels.RemoveRange(e.OldStartingIndex, e.OldItems!.Count);

						break;

					case NotifyCollectionChangedAction.Replace:

						for (int i = e.OldStartingIndex; i < e.OldStartingIndex + e.OldItems!.Count; i++)
							OnMessageViewModelRemoved(MessageViewModels[i]);
						MessageViewModels.RemoveRange(e.OldStartingIndex, e.OldItems!.Count);

						MessageViewModels.InsertRange(e.NewStartingIndex,
							e.NewItems!.Cast<BranchedMessage>().Select(CreateMessageViewModel));

						break;

					case NotifyCollectionChangedAction.Move:

						MessageViewModels.Move(e.OldStartingIndex, e.NewStartingIndex);

						break;

					case NotifyCollectionChangedAction.Reset:

						MessageViewModels.Reset(Chat.Messages.Select(CreateMessageViewModel));

						break;
				}
				;
			});
		}

		private MessageViewModelBase CreateMessageViewModel(BranchedMessage branchedMessage)
		{
			var message = branchedMessage.Message;

			if (message is UserMessage)
			{
				return new UserMessageViewModel(branchedMessage, Chat);
			}
			else if (message is AssistantMessage)
			{
				return new AssistantMessageViewModel(branchedMessage, Chat);
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
	}
}