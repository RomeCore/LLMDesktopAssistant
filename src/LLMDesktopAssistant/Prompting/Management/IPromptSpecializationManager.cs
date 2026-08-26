namespace LLMDesktopAssistant.Prompting.Management
{
	/// <summary>
	/// Manages <see cref="PromptSpecialization"/> instances imported from templates and configuration.
	/// </summary>
	public interface IPromptSpecializationManager : IPromptPartManager<Guid, PromptSpecialization>
	{
	}
}
