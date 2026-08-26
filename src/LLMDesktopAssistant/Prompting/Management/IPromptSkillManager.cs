namespace LLMDesktopAssistant.Prompting.Management
{
	/// <summary>
	/// Manages <see cref="PromptSkill"/> instances imported from templates and configuration.
	/// </summary>
	public interface IPromptSkillManager : IPromptPartManager<string, PromptSkill>
	{
	}
}
