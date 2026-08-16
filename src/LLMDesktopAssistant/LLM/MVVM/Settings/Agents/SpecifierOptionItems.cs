using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Tools.Specifiers;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents;

/// <summary>
/// Represents a <see cref="SpecifierDecision"/> value with a localized display name for use in ComboBox.
/// </summary>
public class SpecifierDecisionItem
{
	/// <summary>
	/// The <see cref="SpecifierDecision"/> value.
	/// </summary>
	public SpecifierDecision Value { get; }

	/// <summary>
	/// Localized display name.
	/// </summary>
	public string DisplayName { get; }

	public SpecifierDecisionItem(SpecifierDecision value)
	{
		Value = value;
		var key = $"tool.specifier.decision.{value.ToString().ToLower()}";
		DisplayName = LocalizationManager.LocalizeStatic(key);

		// Fallback to enum name if localization missing
		if (DisplayName == key || string.IsNullOrEmpty(DisplayName))
			DisplayName = value.ToString();
	}

	/// <summary>
	/// Gets all <see cref="SpecifierDecision"/> values with localized display names.
	/// </summary>
	public static ImmutableList<SpecifierDecisionItem> All { get; } =
		Enum.GetValues<SpecifierDecision>()
			.Select(v => new SpecifierDecisionItem(v))
			.ToImmutableList();
}

/// <summary>
/// Represents a <see cref="SpecifierBehaviourUnionMode"/> value with a localized display name for use in ComboBox.
/// </summary>
public class SpecifierUnionModeItem
{
	/// <summary>
	/// The <see cref="SpecifierBehaviourUnionMode"/> value.
	/// </summary>
	public SpecifierBehaviourUnionMode Value { get; }

	/// <summary>
	/// Localized display name.
	/// </summary>
	public string DisplayName { get; }

	public SpecifierUnionModeItem(SpecifierBehaviourUnionMode value)
	{
		Value = value;
		var key = $"tool.specifier.union_mode.{value.ToString().ToLower()}";
		DisplayName = LocalizationManager.LocalizeStatic(key);

		// Fallback to enum name if localization missing
		if (DisplayName == key || string.IsNullOrEmpty(DisplayName))
			DisplayName = value.ToString();
	}

	/// <summary>
	/// Gets all <see cref="SpecifierBehaviourUnionMode"/> values with localized display names.
	/// </summary>
	public static ImmutableList<SpecifierUnionModeItem> All { get; } =
		Enum.GetValues<SpecifierBehaviourUnionMode>()
			.Select(v => new SpecifierUnionModeItem(v))
			.ToImmutableList();
}

/// <summary>
/// Represents a <see cref="SpecifierAggregationMode"/> value with a localized display name for use in ComboBox.
/// </summary>
public class SpecifierAggregationModeItem
{
	/// <summary>
	/// The <see cref="SpecifierAggregationMode"/> value.
	/// </summary>
	public SpecifierAggregationMode Value { get; }

	/// <summary>
	/// Localized display name.
	/// </summary>
	public string DisplayName { get; }

	public SpecifierAggregationModeItem(SpecifierAggregationMode value)
	{
		Value = value;
		var key = $"tool.specifier.aggregation_mode.{value.ToString().ToLower()}";
		DisplayName = LocalizationManager.LocalizeStatic(key);

		// Fallback to enum name if localization missing
		if (DisplayName == key || string.IsNullOrEmpty(DisplayName))
			DisplayName = value.ToString();
	}

	/// <summary>
	/// Gets all <see cref="SpecifierAggregationMode"/> values with localized display names.
	/// </summary>
	public static ImmutableList<SpecifierAggregationModeItem> All { get; } =
		Enum.GetValues<SpecifierAggregationMode>()
			.Select(v => new SpecifierAggregationModeItem(v))
			.ToImmutableList();
}
