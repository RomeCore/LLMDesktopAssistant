namespace LLMDesktopAssistant.Prompting.Management
{
	/// <summary>
	/// Manages <see cref="PromptSubAgent"/> instances imported from templates and configuration.
	/// </summary>
	public interface IPromptSubAgentManager : IPromptPartManager<string, PromptSubAgent>
	{
	}
}
