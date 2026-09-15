using LLMDesktopAssistant.SourceGenerators;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Chat-level sub-agent settings.
	/// The sub-agent addon sources are configured by the shared addons settings
	/// (<see cref="ChatAddonSettings"/>, the 'agents' addon type).
	/// </summary>
	[SettingsRoute(nameof(ChatSettings.SubAgents))]
	public partial class ChatSubAgentSettings : ChatSettingsCategoryBase
	{
	}
}
