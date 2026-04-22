namespace LLMDesktopAssistant.LLM.Settings
{
	[ViewModelFor(typeof(ChatSummarizationSettingsView))]
	public class ChatSummarizationSettingsViewModel : ViewModelBase
	{
		public ChatSummarizationSettings SummarizationSettings { get; }

		public ChatSummarizationSettingsViewModel(ChatSummarizationSettings settings)
		{
			SummarizationSettings = settings;
		}
	}
}
