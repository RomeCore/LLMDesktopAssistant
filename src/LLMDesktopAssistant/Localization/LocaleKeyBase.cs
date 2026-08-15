using System.ComponentModel;

namespace LLMDesktopAssistant.Localization
{
	public abstract class LocaleKeyBase : INotifyPropertyChanged
	{
		/// <summary>
		/// Gets the full localization key.
		/// </summary>
		public string Key { get; }

		/// <summary>
		/// Gets the localized value for the current language.
		/// </summary>
		public string Value => RawValue ?? Key;

		/// <summary>
		/// Gets the localized value for the current language. Returns null if key is not found.
		/// </summary>
		public abstract string? RawValue { get; }

		protected static readonly PropertyChangedEventArgs _cachedPropertyChangedArgs = new(nameof(Value));

		public abstract event PropertyChangedEventHandler? PropertyChanged;

		public LocaleKeyBase(string key)
		{
			Key = key;
		}

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

	}
}
