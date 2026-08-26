namespace LLMDesktopAssistant.Settings.Application
{
	/// <summary>
	/// Language settings category of the application.
	/// </summary>
	public class ApplicationLanguageSettings : ApplicationSettingsCategoryBase
	{
		private string _system = string.Empty;
		/// <summary>
		/// Gets or sets the system language of the application. The value is a locale code
		/// (for example <c>ru-RU</c>), or an empty string for the neutral locale.
		/// </summary>
		public string System
		{
			get => _system;
			set => SetProperty(ref _system, value);
		}

		private string? _prompt = null;
		/// <summary>
		/// Gets or sets the language that will be used for retrieving prompts. The value is a locale code
		/// (for example <c>ru-RU</c>), an empty string for the neutral locale, or <c>null</c> for system language.
		/// </summary>
		public string? Prompt
		{
			get => _prompt;
			set => SetProperty(ref _prompt, value);
		}
	}
}
