namespace LLMDesktopAssistant.Prompting.Skills
{
	public interface ISkillLoader
	{
		/// <summary>
		/// Loads skills from the provided file paths.
		/// </summary>
		/// <param name="files">The file paths to load skills from.</param>
		/// <returns>A list of loaded skills.</returns>
		IEnumerable<SkillInfo> Load(IEnumerable<SkillFileInfo> files);
	}
}
