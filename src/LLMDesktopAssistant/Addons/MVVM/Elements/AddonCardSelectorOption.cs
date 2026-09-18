using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Addons.MVVM.Elements
{
	/// <summary>
	/// The non-generic view of a selector option: everything the option template of the selector
	/// needs to render, without the knowledge of the option value type.
	/// </summary>
	public interface IAddonCardSelectorOption
	{
		/// <summary>
		/// Gets the localized display name of the option.
		/// </summary>
		LocaleKeyBase DisplayName { get; }
	}

	/// <summary>
	/// An option of an <see cref="AddonCardSelectorChange{TAddon, TChange, TValue}"/>.
	/// </summary>
	/// <typeparam name="TValue">The type of the option value.</typeparam>
	public sealed class AddonCardSelectorOption<TValue> : IAddonCardSelectorOption
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AddonCardSelectorOption{TValue}"/> class.
		/// </summary>
		/// <param name="value">The value the option stands for.</param>
		/// <param name="displayName">The localized display name of the option.</param>
		public AddonCardSelectorOption(TValue value, LocaleKeyBase displayName)
		{
			Value = value;
			DisplayName = displayName;
		}

		/// <summary>
		/// Gets the value the option stands for.
		/// </summary>
		public TValue Value { get; }

		/// <summary>
		/// Gets the localized display name of the option.
		/// </summary>
		public LocaleKeyBase DisplayName { get; }
	}
}
