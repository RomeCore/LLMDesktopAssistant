using System.Globalization;
using System.Resources;
using LLMDesktopAssistant.Localization.Resources;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Settings.Application;

namespace LLMDesktopAssistant.Localization
{
	[Service(typeof(LocalizationManager))]
	public class ResxLocalizationManager : LocalizationManager
	{
		private readonly ImmutableDictionary<string, CultureInfo?> _languageMap;
		private readonly ResourceManager _resourceManager;
		private CultureInfo? _currentCulture;

		public ResxLocalizationManager()
		{
			var builder = ImmutableDictionary.CreateBuilder<string, CultureInfo?>();

			builder.Add("", null); // Neutral culture
			builder.Add("English (US)", new CultureInfo("en-US"));
			builder.Add("Русский (Россия)", new CultureInfo("ru-RU"));

			_languageMap = builder.ToImmutable();

			_resourceManager = Locale.ResourceManager;

			if (_languageMap.ContainsValue(CultureInfo.CurrentUICulture))
				_currentCulture = CultureInfo.CurrentUICulture;
			else
				_currentCulture = null; // Default to neutral culture if current UI culture is not supported
			Locale.Culture = _currentCulture;

			// Apply the language saved in the application settings, if any.
			var savedLanguage = ApplicationSettingsAccessor.ApplicationSettings.Language.System;
			if (!string.IsNullOrEmpty(savedLanguage))
				CurrentLanguage = savedLanguage;
		}

		public override IEnumerable<string> GetAvailableLanguages()
		{
			return _languageMap.Keys;
		}

		public override string Localize(string key)
		{
			return _resourceManager.GetString(key, _currentCulture) ?? key;
		}

		protected override bool TryChangeLanguage(string language)
		{
			if (_languageMap.TryGetValue(language, out var culture))
			{
				_currentCulture = culture;
				Locale.Culture = _currentCulture;
				ApplicationSettingsAccessor.ApplicationSettings.Language.System = language;
				return true;
			}
			return false;
		}
	}
}