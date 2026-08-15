using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Represents a selectable inheritance scope for a chat settings group.
	/// </summary>
	public class InheritanceLevelItem
	{
		/// <summary>
		/// The inheritance level value.
		/// </summary>
		public ChatSettingsInheritanceLevel Value { get; init; }

		/// <summary>
		/// The localized display name of the inheritance level.
		/// </summary>
		public string DisplayName { get; init; } = string.Empty;

		/// <inheritdoc/>
		public override bool Equals(object? obj) => obj is InheritanceLevelItem other && Value == other.Value;

		/// <inheritdoc/>
		public override int GetHashCode() => Value.GetHashCode();

		static InheritanceLevelItem()
		{
			AllProfile = [
				new() { Value = ChatSettingsInheritanceLevel.Profile, DisplayName = LocalizationManager.LocalizeStatic("settings_scope_profile") },
				new() { Value = ChatSettingsInheritanceLevel.Application, DisplayName = LocalizationManager.LocalizeStatic("settings_scope_application") },
			];

			AllAgent = [
				new() { Value = ChatSettingsInheritanceLevel.Agent, DisplayName = LocalizationManager.LocalizeStatic("settings_scope_agent") },
				.. AllProfile,
			];
		}

		public static readonly ImmutableList<InheritanceLevelItem> AllProfile;

		public static readonly ImmutableList<InheritanceLevelItem> AllAgent;
	}
}
