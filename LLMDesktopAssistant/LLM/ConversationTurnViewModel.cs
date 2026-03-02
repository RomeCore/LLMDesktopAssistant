using System.Collections.ObjectModel;
using LLMDesktopAssistant.MVVM;
using RCLargeLanguageModels.Messages;

namespace LLMDesktopAssistant.LLM
{
	public enum ConversationTurnState
	{
		None,
		Processing,
		Complete
	}

	[ViewModelFor(typeof(ConversationTurnView))]
	public class ConversationTurnViewModel : ViewModelBase
	{
		private UserMessageViewModel? _userMessage;
		/// <summary>
		/// Gets or sets the user message associated with this conversation turn. Used for display purposes.
		/// </summary>
		public UserMessageViewModel? UserMessage
		{
			get => _userMessage;
			set => SetProperty(ref _userMessage, value);
		}

		private ObservableCollection<AssistantMessagePartViewModel> _assistantMessageParts = [];
		/// <summary>
		/// Collection of message parts that make up the assistant's response. Used for display purposes.
		/// </summary>
		public ObservableCollection<AssistantMessagePartViewModel> AssistantMessageParts
		{
			get => _assistantMessageParts;
			set => SetProperty(ref _assistantMessageParts, value);
		}

		private ConversationTurnState _state;
		public ConversationTurnState State
		{
			get => _state;
			set => SetProperty(ref _state, value);
		}

		private ObservableCollection<IMessage> _messages = [];
		/// <summary>
		/// Collection of messages associated with this conversation turn. Used for LLM generation purposes.
		/// </summary>
		public ObservableCollection<IMessage> Messages
		{
			get => _messages;
			set => SetProperty(ref _messages, value);
		}

		public ConversationTurnViewModel()
		{
		}
	}
}