namespace LLMDesktopAssistant.Prompting.Skills
{
	/// <summary>
	/// Represents information about skill file, including its name and source.
	/// </summary>
	/// <param name="FileName">The full path to the skill file.</param>
	/// <param name="Source">The source of the skill.</param>
	public readonly record struct SkillFileInfo(string FileName, SkillSource Source);
}
