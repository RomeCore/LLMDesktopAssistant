using System.Globalization;
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
		/// Gets the locale code of the language as used by <see cref="LocalizationManager"/>.
		/// An empty value means the neutral (invariant) locale.
		/// </summary>
		public required string Name { get; init; }

		/// <summary>
		/// Gets the display name of the language. For the neutral locale option, a localized name is shown.
		/// </summary>
		public string DisplayName
		{
			get
			{
				if (string.IsNullOrEmpty(Name))
					return LocalizationManager.LocalizeStatic("settings.language.system");

				try
				{
					return CultureInfo.GetCultureInfo(Name).DisplayName;
				}
				catch (CultureNotFoundException)
				{
					return Name;
				}
			}
		}
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
				.Select(code => new LanguageItemViewModel { Name = code })];
			_currentLanguage = AvailableLanguages.FirstOrDefault(l => l.Name == _localizationManager.CurrentLanguage)
				?? AvailableLanguages[0];
		}
	}
}
