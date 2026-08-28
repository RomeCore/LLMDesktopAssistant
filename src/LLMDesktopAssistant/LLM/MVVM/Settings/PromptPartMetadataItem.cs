using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// A metadata entry of a prompt part with a localized key display name.
/// </summary>
public class PromptPartMetadataItem
{
	/// <summary>
	/// Gets the localization key of the metadata key display name.
	/// </summary>
	public LocaleKeyBase Key { get; }

	/// <summary>
	/// Gets the metadata value.
	/// </summary>
	public string Value { get; }

	public PromptPartMetadataItem(LocaleKeyBase key, string value)
	{
		Key = key;
		Value = value;
	}
}
