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

		public UserMessageViewModel()
		{
		}

		public UserMessageViewModel(IUserMessage message)
		{
			Text = message.Content ?? string.Empty;
		}
	}
}