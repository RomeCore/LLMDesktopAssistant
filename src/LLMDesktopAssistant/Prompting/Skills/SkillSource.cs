namespace LLMDesktopAssistant.Prompting.Skills
{
	public enum SkillSource
	{
		Unknown,

		/// <summary>
		/// The skill is defined in .llt template files.
		/// </summary>
		Template,

		/// <summary>
		/// The skill is defined in the %LOCALAPPDATA%/.llmassist/skills/ or ~/*agent_home*/skills/ directories.
		/// </summary>
		UserProfile,

		/// <summary>
		/// The skill is defined in the ./*agent_home*/skills/ directories.
		/// </summary>
		WorkingDirectory,

		/// <summary>
		/// The skill is defined in a custom location.
		/// </summary>
		Custom
	}
}
