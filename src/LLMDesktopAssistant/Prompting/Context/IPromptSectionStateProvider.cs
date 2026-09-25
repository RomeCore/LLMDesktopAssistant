using LLMDesktopAssistant.Agents;

namespace LLMDesktopAssistant.Prompting.Context
{
	public interface IPromptSectionStateProvider<TState>
		where TState : PromptSectionStateBase
	{
		TState? CaptureState(ChatAgentDescriptor agent);
	}
}
