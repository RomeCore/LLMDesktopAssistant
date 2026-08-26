namespace LLMDesktopAssistant.Prompting.Management
{
	/// <summary>
	/// Manages <see cref="PromptPersona"/> instances imported from templates and configuration.
	/// </summary>
	public interface IPromptPersonaManager : IPromptPartManager<Guid, PromptPersona>
	{
	}
}
