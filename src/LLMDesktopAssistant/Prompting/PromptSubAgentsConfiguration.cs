using LLMDesktopAssistant.Settings;

namespace LLMDesktopAssistant.Prompting
{
	/// <summary>
	/// Represents the configuration of sub-agent prompts.
	/// </summary>
	[SettingsObject("prompt_sub_agents")]
	public class PromptSubAgentsConfiguration : PromptPartConfigurationBase<PromptSubAgent>
	{
	}
}
