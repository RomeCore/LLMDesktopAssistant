using System.Collections.Concurrent;
using System.ComponentModel;

namespace LLMDesktopAssistant.Localization
{
	/// <summary>
	/// Represents a localization key that resolves to its current localized value.
	/// Instances are cached per key and implement <see cref="INotifyPropertyChanged"/> so that
	/// bindings to <see cref="Value"/> are updated automatically when the language changes.
	/// </summary>
	public sealed class LocaleKey : INotifyPropertyChanged, IEquatable<LocaleKey>
	{
		private static readonly ConcurrentDictionary<string, LocaleKey> Cache = new();

		private LocaleKey(string key)
		{
			Key = key;
			LocalizationManager.StaticLanguageChanged += OnStaticLanguageChanged;
		}

		/// <summary>
		/// Gets or creates the cached <see cref="LocaleKey"/> instance for the specified key.
		/// </summary>
		/// <param name="key">The full localization key.</param>
		/// <returns>The cached instance for the key.</returns>
		public static LocaleKey GetOrCreate(string key)
		{
			return Cache.GetOrAdd(key, static k => new LocaleKey(k));
		}

		/// <summary>
		/// Gets the full localization key.
		/// </summary>
		public string Key { get; }

		/// <summary>
		/// Gets the localized value for the current language.
		/// </summary>
		public string Value => LocalizationManager.LocalizeStatic(Key);

		/// <summary>
		/// Gets the localized value for the current language. Returns null if key is not found.
		/// </summary>
		public string? RawValue => LocalizationManager.TryLocalizeStatic(Key);

		/// <summary>
		/// Formats the localized value with the specified arguments using <see cref="string.Format(string, object?[])"/>.
		/// </summary>
		/// <param name="args">The arguments to substitute into the format string.</param>
		/// <returns>The formatted localized string.</returns>
		public string Format(params object?[] args)
		{
			return string.Format(Value, args);
		}

		/// <summary>
		/// Returns the localized value of the key.
		/// </summary>
		/// <returns>The localized string.</returns>
		public override string ToString()
		{
			return Value;
		}

		/// <inheritdoc />
		public bool Equals(LocaleKey? other)
		{
			return other is not null && string.Equals(Key, other.Key, StringComparison.Ordinal);
		}

		/// <inheritdoc />
		public override bool Equals(object? obj)
		{
			return Equals(obj as LocaleKey);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return StringComparer.Ordinal.GetHashCode(Key);
		}

		/// <inheritdoc />
		public event PropertyChangedEventHandler? PropertyChanged;

		private static readonly PropertyChangedEventArgs _cachedPropertyChangedArgs = new(nameof(Value));

		private void OnStaticLanguageChanged(object? sender, string language)
		{
			PropertyChanged?.Invoke(this, _cachedPropertyChangedArgs);
		}
	}
}
