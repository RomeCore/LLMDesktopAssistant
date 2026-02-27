using LLMDesktopAssistant.MVVM;
using RCLargeLanguageModels.Messages;

namespace LLMDesktopAssistant.LLM
{
	[ViewModelFor(typeof(AssistantMessageTextPartView))]
	public class AssistantMessageTextPartViewModel : AssistantMessagePartViewModel
	{
		private string _text = string.Empty;
		public string Text
		{
			get => _text;
			set => SetProperty(ref _text, value);
		}

		public AssistantMessageTextPartViewModel()
		{
		}

		public AssistantMessageTextPartViewModel(IAssistantMessage message)
		{
			Text = message.Content ?? string.Empty;
			if (message is PartialAssistantMessage pMessage)
			{
				pMessage.PartAdded += (s, e) =>
				{
					App.Current.Dispatcher.Invoke(() =>
					{
						Text = message.Content ?? string.Empty;
					});
				};
			}
		}
	}
}