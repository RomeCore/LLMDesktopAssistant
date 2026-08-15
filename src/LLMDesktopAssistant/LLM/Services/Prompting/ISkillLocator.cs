namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	public interface ISkillLocator
	{
		/// <summary>
		/// Locates all skill files based on current configuration.
		/// </summary>
		/// <returns>A list of skill file (SKILL.md) paths.</returns>
		IEnumerable<string> LocateSkillFiles();
	}
}
