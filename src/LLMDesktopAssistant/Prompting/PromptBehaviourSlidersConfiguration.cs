using LLMDesktopAssistant.Settings;

namespace LLMDesktopAssistant.Prompting
{
	/// <summary>
	/// Represents the configuration of behaviour sliders.
	/// </summary>
	[SettingsObject("prompt_behaviour_sliders")]
	public class PromptBehaviourSlidersConfiguration : PromptPartConfigurationBase<PromptBehaviourSlider>
	{
	}
}
