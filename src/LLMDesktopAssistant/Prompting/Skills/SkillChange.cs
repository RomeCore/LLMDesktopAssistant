using LLMDesktopAssistant.Addons;

namespace LLMDesktopAssistant.Prompting.Skills
{
	/// <summary>
	/// The per-skill change (configuration) object used by the agent's skillset settings.
	/// </summary>
	public class SkillChange : AddonChangeBase
	{
		private SkillInjectionMode? _injectionMode;
		/// <summary>
		/// The mode for injecting the skill into the prompt. Null indicates that the setting has not been changed yet.
		/// </summary>
		public SkillInjectionMode? InjectionMode
		{
			get => _injectionMode;
			set => SetProperty(ref _injectionMode, value);
		}
	}
}
