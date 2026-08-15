using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// Represents a ToolApprovalLevel value with a localized display name for use in ComboBox.
/// </summary>
public class ToolApprovalLevelItem
{
	/// <summary>
	/// The ToolApprovalLevel value.
	/// </summary>
	public ToolApprovalLevel Value { get; }

	/// <summary>
	/// Localized display name.
	/// </summary>
	public string DisplayName { get; }

	public ToolApprovalLevelItem(ToolApprovalLevel value)
	{
		Value = value;
		var key = $"approval_level_{value.ToString().ToLower()}";
		DisplayName = LocalizationManager.LocalizeStatic(key);

		// Fallback to enum name if localization missing
		if (DisplayName == key || string.IsNullOrEmpty(DisplayName))
			DisplayName = value.ToString();
	}

	/// <summary>
	/// Gets all ToolApprovalLevel values with localized display names.
	/// </summary>
	public static ImmutableList<ToolApprovalLevelItem> All { get; } =
		Enum.GetValues<ToolApprovalLevel>()
			.Select(v => new ToolApprovalLevelItem(v))
			.ToImmutableList();
}
