using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.Core.Localization.Resources;
using LLMDesktopAssistant.Core.Modules;

namespace LLMDesktopAssistant.Core.Localization
{
	[Module(Order = 1)]
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
				return true;
			}
			return false;
		}
	}
}