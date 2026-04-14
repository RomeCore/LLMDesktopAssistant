using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Core.Services;

namespace LLMDesktopAssistant.Core.Localization
{
	/// <summary>
	/// Manages localization for the application.
	/// </summary>
	public abstract class LocalizationManager : NotifyPropertyChanged
	{
		/// <summary>
		/// Localizes a given key using the current localization manager.
		/// </summary>
		/// <param name="key">The key to localize.</param>
		/// <returns>The localized string, or the original key if not found.</returns>
		public static string LocalizeStatic(string key)
		{
			return ServiceRegistry.TryGet<LocalizationManager>()?.Localize(key) ?? key;
		}

		/// <summary>
		/// Event that is raised when the language changes.
		/// </summary>
		public event EventHandler<string>? LanguageChanged;

		private string _currentLanguage = string.Empty;
		/// <summary>
		/// Gets or sets the current language.
		/// </summary>
		public string CurrentLanguage
		{
			get => _currentLanguage;
			set
			{
				if (_currentLanguage != value)
				{
					if (TryChangeLanguage(value))
					{
						SetProperty(ref _currentLanguage, value);
						LanguageChanged?.Invoke(this, _currentLanguage);
					}
				}
			}
		}

		/// <summary>
		/// Localizes a given key to the current language.
		/// </summary>
		/// <param name="key">The key to localize.</param>
		/// <returns>The localized value. If the key is not found, returns the original key.</returns>
		public abstract string Localize(string key);

		/// <summary>
		/// Gets a list of available languages. Languages are represented in human-readable format, for example "English (US)" or "Русский (Россия)".
		/// </summary>
		/// <returns>A list of available languages.</returns>
		public abstract IEnumerable<string> GetAvailableLanguages();

		/// <summary>
		/// Tries to change the current language. Languages are represented in human-readable format, for example "English (US)" or "Русский (Россия)", they are listed in <see cref="GetAvailableLanguages()"/>.
		/// </summary>
		/// <param name="language">The language to try and set.</param>
		/// <returns>true if the language was changed; otherwise, false.</returns>
		protected abstract bool TryChangeLanguage(string language);
	}

	/// <summary>
	/// A dummy localization manager that does nothing. This is used as a fallback if no other localization manager is available.
	/// </summary>
	[Service(Order = int.MaxValue)]
	public class DummyLocalizationManager : LocalizationManager
	{
		public override IEnumerable<string> GetAvailableLanguages()
		{
			return [];
		}

		public override string Localize(string key)
		{
			return key;
		}

		protected override bool TryChangeLanguage(string language)
		{
			return false;
		}
	}
}