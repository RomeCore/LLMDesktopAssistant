using System.Globalization;
using System.Reflection;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Settings.Application;
using LLMDesktopAssistant.Utils;
using Serilog;

namespace LLMDesktopAssistant.Localization
{
	/// <summary>
	/// A <see cref="LocalizationManager"/> that loads localized strings from .loc files.
	/// Files are loaded from the embedded resources of the current assembly and from the user locale
	/// directory (<c>%LOCALAPPDATA%\.llmassist\locale</c>), where user files override embedded ones.
	/// </summary>
	[Service(typeof(LocalizationManager), Order = 10)]
	public class LocFileLocalizationManager : LocalizationManager
	{
		private readonly Assembly _resourceAssembly;
		private readonly Dictionary<string, Dictionary<string, string>> _entriesByLocale = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, string> _languageMap = new(StringComparer.Ordinal);
		private string _currentLocale = string.Empty;

		/// <summary>
		/// Initializes a new instance of the <see cref="LocFileLocalizationManager"/> class.
		/// </summary>
		/// <param name="userLocaleDirectory">The directory with user .loc files, or <see langword="null"/> to use the default location.</param>
		/// <param name="resourceAssembly">The assembly whose embedded .loc resources are loaded, or <see langword="null"/> to use the current assembly.</param>
		public LocFileLocalizationManager(string? userLocaleDirectory = null, Assembly? resourceAssembly = null)
		{
			_resourceAssembly = resourceAssembly ?? Assembly.GetExecutingAssembly();
			LoadEmbeddedFiles();
			LoadUserFiles(userLocaleDirectory ?? Directories.LocaleFiles);
			InitializeLanguageMap();

			var savedLanguage = ApplicationSettingsAccessor.ApplicationSettings.Language.System;
			if (!string.IsNullOrEmpty(savedLanguage))
				CurrentLanguage = savedLanguage;
		}

		/// <inheritdoc />
		public override string? TryLocalize(string key)
		{
			if (_entriesByLocale.TryGetValue(_currentLocale, out var entries) && entries.TryGetValue(key, out var value))
				return value;

			// Fall back to the neutral locale entries before asking the fallback manager.
			if (!string.IsNullOrEmpty(_currentLocale)
				&& _entriesByLocale.TryGetValue(string.Empty, out var neutralEntries)
				&& neutralEntries.TryGetValue(key, out value))
				return value;

			return null;
		}

		/// <inheritdoc />
		public override IEnumerable<string> GetAvailableLanguages()
		{
			return _languageMap.Keys;
		}

		/// <inheritdoc />
		protected override bool TryChangeLanguage(string language)
		{
			if (_languageMap.TryGetValue(language, out var locale))
			{
				_currentLocale = locale;
				ApplicationSettingsAccessor.ApplicationSettings.Language.System = language;
				return true;
			}
			return false;
		}

		private void LoadEmbeddedFiles()
		{
			var assembly = _resourceAssembly;
			foreach (var resourceName in assembly.GetManifestResourceNames()
				.Where(n => n.EndsWith(".loc", StringComparison.OrdinalIgnoreCase))
				.OrderBy(n => n, StringComparer.Ordinal))
			{
				using var stream = assembly.GetManifestResourceStream(resourceName);
				if (stream == null)
					continue;

				using var reader = new StreamReader(stream);
				AddFile(reader.ReadToEnd(), $"embedded:{resourceName}");
			}
		}

		private void LoadUserFiles(string directory)
		{
			if (!Directory.Exists(directory))
				return;

			foreach (var file in Directory.GetFiles(directory, "*.loc", SearchOption.AllDirectories)
				.OrderBy(f => f, StringComparer.Ordinal))
			{
				try
				{
					AddFile(File.ReadAllText(file), file);
				}
				catch (Exception ex)
				{
					Log.Warning(ex, "Failed to load locale file {File}.", file);
				}
			}
		}

		private void AddFile(string content, string source)
		{
			LocFileDocument document;
			try
			{
				document = LocFileParser.Parse(content);
			}
			catch (Exception ex)
			{
				Log.Warning(ex, "Failed to parse locale file {Source}.", source);
				return;
			}

			if (!_entriesByLocale.TryGetValue(document.Locale, out var entries))
			{
				entries = new Dictionary<string, string>(StringComparer.Ordinal);
				_entriesByLocale[document.Locale] = entries;
			}

			foreach (var (key, value) in document.Entries)
			{
				if (!entries.TryAdd(key, value))
				{
					if (source.StartsWith("embedded:", StringComparison.Ordinal))
						Log.Warning("Duplicate key '{Key}' in embedded locale file {Source}.", key, source);
					else
						entries[key] = value; // User files override embedded and earlier user files.
				}
			}
		}

		private void InitializeLanguageMap()
		{
			// Then add the locales that appear in .loc files.
			foreach (var locale in _entriesByLocale.Keys.OrderBy(l => l, StringComparer.Ordinal))
			{
				var displayName = GetDisplayName(locale);
				_languageMap.TryAdd(displayName, locale);
			}
		}

		private static string GetDisplayName(string locale)
		{
			if (string.IsNullOrEmpty(locale))
				return "English (US)";

			try
			{
				return CultureInfo.GetCultureInfo(locale).DisplayName;
			}
			catch (CultureNotFoundException)
			{
				return locale;
			}
		}
	}
}
