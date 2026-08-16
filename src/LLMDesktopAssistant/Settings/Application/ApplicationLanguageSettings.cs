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
		/// Gets or sets the system language of the application. The value is a locale code
		/// (for example <c>ru-RU</c>), or an empty string for the neutral locale.
		/// </summary>
		public string System
		{
			get => _system;
			set => SetProperty(ref _system, value);
		}
	}
}
