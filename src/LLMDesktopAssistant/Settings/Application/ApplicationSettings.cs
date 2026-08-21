using LLMDesktopAssistant.LLM.Settings;

namespace LLMDesktopAssistant.Settings.Application
{
	[SettingsObject("application")]
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

		private ApplicationLanguageSettings _language = new();
		/// <summary>
		/// Gets or sets the language settings of the application.
		/// </summary>
		public ApplicationLanguageSettings Language
		{
			get => _language;
			set => SetProperty(ref _language, value);
		}

		private WebFetchSettings _webFetch = new();
		/// <summary>
		/// Gets or sets the web fetching settings of the application.
		/// </summary>
		public WebFetchSettings WebFetch
		{
			get => _webFetch;
			set => SetProperty(ref _webFetch, value);
		}
	}
}
