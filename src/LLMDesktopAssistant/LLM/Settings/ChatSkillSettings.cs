using LLMDesktopAssistant.SourceGenerators;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Chat-level skills settings.
	/// </summary>
	[SettingsRoute(nameof(ChatSettings.Skills))]
	public partial class ChatSkillSettings : ChatSettingsCategoryBase
	{
	}
}
