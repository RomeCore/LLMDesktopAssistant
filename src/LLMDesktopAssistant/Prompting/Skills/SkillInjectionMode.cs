namespace LLMDesktopAssistant.Prompting.Skills
{
	public enum SkillInjectionMode
	{
		/// <summary>
		/// Only inject the skill name and description into the prompt.
		/// </summary>
		Default,

		/// <summary>
		/// Inject the SKILL.md content along with name and description into the prompt.
		/// </summary>
		Full
	}
}
