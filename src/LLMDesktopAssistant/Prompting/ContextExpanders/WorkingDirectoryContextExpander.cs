using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;

namespace LLMDesktopAssistant.Prompting.ContextExpanders
{
	[ChatService(typeof(IPromptSystemContextExpander))]
	public class WorkingDirectoryContextExpander(IChatSettingsService chatSettings) : IPromptSystemContextExpander
	{
		public void ExpandPromptContext(Dictionary<string, object?> context)
		{
			context["working_directory"] = chatSettings.Settings.Environment.GetEffectiveWorkingDirectories().GetWorkingDirectory();
		}
	}
}