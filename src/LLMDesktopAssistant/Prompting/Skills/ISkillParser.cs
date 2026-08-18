namespace LLMDesktopAssistant.Prompting.Skills
{
	public interface ISkillParser
	{
		SkillInfo Parse(string fullpath, string contents, SkillSource source = SkillSource.Unknown);
	}
}
