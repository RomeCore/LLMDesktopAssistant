using LLMDesktopAssistant.Agents;

namespace LLMDesktopAssistant.Prompting.State
{
	public interface IPromptSectionStateProvider<TState>
		where TState : PromptSectionStateBase
	{
		TState GetState(ChatAgentDescriptor agent);
	}
}
