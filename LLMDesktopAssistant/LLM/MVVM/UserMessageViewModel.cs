using LLMDesktopAssistant.LLM.Data;
using LLMDesktopAssistant.MVVM;
using RCLargeLanguageModels.Messages;

namespace LLMDesktopAssistant.LLM.MVVM
{
	[ViewModelFor(typeof(UserMessageView))]
	public class UserMessageViewModel : MessageViewModelBase
	{
		private string _text = string.Empty;
		public string Text
		{
			get => _text;
			set => SetProperty(ref _text, value);
		}

		public UserMessageViewModel(ConversationMessage conversationMessage)
		{
			if (conversationMessage.Message.Message is not IUserMessage userMessage)
				throw new InvalidOperationException("Invalid message type. Expected IUserMessage.");

			Text = userMessage.Content ?? string.Empty;
		}
	}
}