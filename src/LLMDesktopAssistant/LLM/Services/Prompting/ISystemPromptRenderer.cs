using LLMDesktopAssistant.Prompting;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	public interface ISystemPromptRenderer
	{
		SystemPromptSnapshot Render();
	}
}
