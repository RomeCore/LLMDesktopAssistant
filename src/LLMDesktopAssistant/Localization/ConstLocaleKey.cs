using System.ComponentModel;

namespace LLMDesktopAssistant.Localization
{
	public class ConstLocaleKey : LocaleKeyBase, IEquatable<ConstLocaleKey>
	{
		private readonly string _value;

		public override string? RawValue => _value;

		public override event PropertyChangedEventHandler? PropertyChanged
		{
			add { }
			remove { }
		}

		public ConstLocaleKey(string value) : base(value)
		{
			_value = value;
		}

		public static implicit operator string(ConstLocaleKey key)
		{
			return key.Value;
		}

		public static implicit operator ConstLocaleKey(string key)
		{
			return new(key);
		}

		/// <inheritdoc />
		public bool Equals(ConstLocaleKey? other)
		{
			return other is not null && string.Equals(Key, other.Key, StringComparison.Ordinal);
		}

		/// <inheritdoc />
		public override bool Equals(object? obj)
		{
			return Equals(obj as ConstLocaleKey);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return StringComparer.Ordinal.GetHashCode(Key);
		}
	}
}
