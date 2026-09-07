using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Addons
{
	/// <summary>
	/// The addon type descriptor.
	/// </summary>
	public interface IAddonTypeDescriptor
	{
		/// <summary>
		/// Gets the addon type descriptor key (e.g. 'skills').
		/// </summary>
		string Type { get; }

		/// <summary>
		/// Gets the addon CLR type.
		/// </summary>
		Type ClrType { get; }

		/// <summary>
		/// Gets a value indicating whether to use the default diagnostic factory.
		/// </summary>
		bool UseDefaultDiagnosticFactory { get; }

		/// <summary>
		/// Gets the localized display name of the addon type. Used in the addon settings UI.
		/// </summary>
		LocaleKeyBase NameKey { get; }

		/// <summary>
		/// Gets the localized description of the addon type, or <see langword="null"/> when the type has no description.
		/// Used in the addon settings UI.
		/// </summary>
		LocaleKeyBase? DescriptionKey { get; }
	}
}
