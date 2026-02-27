using LLMDesktopAssistant.MVVM;
using RCLargeLanguageModels.Messages;

namespace LLMDesktopAssistant.LLM
{
	[ViewModelFor(typeof(UserMessageView))]
	public class UserMessageViewModel : ViewModelBase
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