using LLMDesktopAssistant.MVVM;
using RCLargeLanguageModels.Messages;

namespace LLMDesktopAssistant.LLM.MVVM
{
	[ViewModelFor(typeof(AssistantMessageReasoningPartView))]
	public class AssistantMessageReasoningPartViewModel : AssistantMessagePartViewModel
	{
		private string _reasoningText = string.Empty;
		public string ReasoningText
		{
			get => _reasoningText;
			set => SetProperty(ref _reasoningText, value);
		}

		public AssistantMessageReasoningPartViewModel()
		{
		}

		public AssistantMessageReasoningPartViewModel(IAssistantMessage message)
		{
			ReasoningText = message.ReasoningContent ?? string.Empty;
			if (message is PartialAssistantMessage pMessage)
			{
				pMessage.PartAdded += (s, e) =>
				{
					App.Current.Dispatcher.Invoke(() =>
					{
						ReasoningText = message.ReasoningContent ?? string.Empty;
					});
				};
			}
		}
	}
}