using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Settings.Application
{
	/// <summary>
	/// Base class for application settings categories.
	/// </summary>
	public class ApplicationSettingsCategoryBase : NotifyPropertyChanged
	{
	}

	/// <summary>
	/// Language settings category of the application.
	/// </summary>
	public class ApplicationLanguageSettings : ApplicationSettingsCategoryBase
	{
		private string _system = string.Empty;
		/// <summary>
		/// Gets or sets the system language of the application. The value is a human-readable language name
		/// as returned by <see cref="LocalizationManager.GetAvailableLanguages()"/>,
		/// for example "English (US)". An empty value means the system language is used.
		/// </summary>
		public string System
		{
			get => _system;
			set => SetProperty(ref _system, value);
		}
	}
}
