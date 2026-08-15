using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Settings.Application
{
	/// <summary>
	/// Represents a language option in the language selection UI.
	/// </summary>
	public class LanguageItemViewModel
	{
		/// <summary>
		/// Gets the language name as used by <see cref="LocalizationManager"/>. An empty value means the system language.
		/// </summary>
		public required string Name { get; init; }

		/// <summary>
		/// Gets the display name of the language. For the system language option, a localized name is shown.
		/// </summary>
		public string DisplayName => string.IsNullOrEmpty(Name)
			? LocalizationManager.LocalizeStatic("settings_language_system")
			: Name;
	}

	/// <summary>
	/// ViewModel for the application language settings category.
	/// </summary>
	[ViewModelFor(typeof(ApplicationLanguageSettingsView))]
	public class ApplicationLanguageSettingsViewModel : ViewModelBase
	{
		private readonly LocalizationManager _localizationManager;

		/// <summary>
		/// Gets the list of available languages.
		/// </summary>
		public RangeObservableCollection<LanguageItemViewModel> AvailableLanguages { get; }

		private LanguageItemViewModel _currentLanguage;
		/// <summary>
		/// Gets or sets the currently selected language. Changing it applies the language immediately.
		/// </summary>
		public LanguageItemViewModel CurrentLanguage
		{
			get => _currentLanguage;
			set
			{
				if (SetProperty(ref _currentLanguage, value) && value is not null)
					_localizationManager.CurrentLanguage = value.Name;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ApplicationLanguageSettingsViewModel"/> class.
		/// </summary>
		public ApplicationLanguageSettingsViewModel()
		{
			_localizationManager = ServiceRegistry.Provider.GetRequiredService<LocalizationManager>();
			AvailableLanguages = [.. _localizationManager.GetAvailableLanguages()
				.Select(name => new LanguageItemViewModel { Name = name })];
			_currentLanguage = AvailableLanguages.FirstOrDefault(l => l.Name == _localizationManager.CurrentLanguage)
				?? AvailableLanguages[0];
		}
	}
}
