namespace LLMDesktopAssistant.LLM.Settings
{
	[ViewModelFor(typeof(ChatModelSettingsView))]
	public class ChatModelSettingsViewModel : ViewModelBase
	{
		public ChatModelSettings ModelSettings { get; }

		public ChatModelSettingsViewModel(ChatModelSettings settings)
		{
			ModelSettings = settings;
		}
	}
}
