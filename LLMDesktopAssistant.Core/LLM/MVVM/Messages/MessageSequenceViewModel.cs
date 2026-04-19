using Avalonia.Collections;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Utils;
using System.Collections.Specialized;

namespace LLMDesktopAssistant.LLM.Messages
{
	[ViewModelFor(typeof(MessageSequenceView))]
	public class MessageSequenceViewModel : ViewModelBase
	{
		// TODO: Maybe change to RangeObservableCollection?
		/// <summary>
		/// Collection of message view models that represent the sequence of messages.
		/// </summary>
		public AvaloniaList<MessageViewModelBase> MessageViewModels { get; }

		/// <summary>
		/// The chat view model instance associated with this message sequence.
		/// </summary>
		public ChatViewModel ChatViewModel { get; }

		public MessageSequenceViewModel(ChatViewModel chatVM)
		{
			MessageViewModels = new AvaloniaList<MessageViewModelBase>();
			ChatViewModel = chatVM;

			MessageViewModels.AddRange(chatVM.Chat.Messages.Select(CreateMessageViewModel));
			chatVM.Chat.Messages.CollectionChanged += OnMessagesCollectionChanged;
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

						MessageViewModels.Clear();
						MessageViewModels.AddRange(ChatViewModel.Chat.Messages.Select(CreateMessageViewModel));

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
				return new UserMessageViewModel(branchedMessage, ChatViewModel);
			}
			else if (message is AssistantMessage)
			{
				return new AssistantMessageViewModel(branchedMessage, ChatViewModel);
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