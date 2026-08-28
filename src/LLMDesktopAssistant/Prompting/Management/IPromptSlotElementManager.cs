namespace LLMDesktopAssistant.Prompting.Management
{
	/// <summary>
	/// Manages <see cref="PromptSlotElement"/> instances imported from templates and configuration.
	/// </summary>
	public interface IPromptSlotElementManager : IPromptPartManager<(Guid, PromptSlotKind), PromptSlotElement>
	{
	}
}
