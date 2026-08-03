using LLMDesktopAssistant.LLM.Settings;

namespace LLMDesktopAssistant.Settings.Application
{
	public class ApplicationSettings : SettingsObject
	{
		private ChatSettings _inheritedChatSettings = new();
		/// <summary>
		/// Gets or sets the chat settings that will be used for inherted chat and agent settings.
		/// </summary>
		public ChatSettings InheritedChatSettings
		{
			get => _inheritedChatSettings;
			set => SetProperty(ref _inheritedChatSettings, value);
		}
	}
}
