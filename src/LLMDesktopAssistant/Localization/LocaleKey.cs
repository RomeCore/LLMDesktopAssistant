using System.Collections.Concurrent;
using System.ComponentModel;

namespace LLMDesktopAssistant.Localization
{
	/// <summary>
	/// Represents a localization key that resolves to its current localized value.
	/// Instances are cached per key and implement <see cref="INotifyPropertyChanged"/> so that
	/// bindings to <see cref="Value"/> are updated automatically when the language changes.
	/// </summary>
	public sealed class LocaleKey : LocaleKeyBase, IEquatable<LocaleKey>
	{
		private static readonly ConcurrentDictionary<string, LocaleKey> Cache = new();

		private bool _isValueCached = false;
		private string? _cachedValue = null;

		public override string? RawValue
		{
			get
			{
				if (!_isValueCached)
				{
					_cachedValue = LocalizationManager.TryLocalizeStatic(Key);
					_isValueCached = true;
					return _cachedValue;
				}
				return _cachedValue;
			}
		}

		private LocaleKey(string key) : base(key)
		{
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

		public static implicit operator string(LocaleKey key)
		{
			return key.Value;
		}

		public static implicit operator LocaleKey(string key)
		{
			return GetOrCreate(key);
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
		public override event PropertyChangedEventHandler? PropertyChanged;

		private void OnStaticLanguageChanged(object? sender, string language)
		{
			_isValueCached = false;
			_cachedValue = null;
			PropertyChanged?.Invoke(this, _cachedValuePropertyChangedArgs);
			PropertyChanged?.Invoke(this, _cachedRawValuePropertyChangedArgs);
		}
	}
}
