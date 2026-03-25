using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.MVVM;

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

		public AssistantMessageReasoningPartViewModel(AssistantMessage message)
		{
			ReasoningText = message.ReasoningContent ?? string.Empty;
			message.PropertyChanged += (s, e) =>
			{
				App.Current.Dispatcher.Invoke(() =>
				{
					ReasoningText = message.ReasoningContent ?? string.Empty;
				});
			};
		}
	}
}