namespace LLMDesktopAssistant.Prompting.State
{
	public interface IPromptSectionStateProvider<TState>
		where TState : PromptSectionStateBase
	{
		TState GetState();
	}


}
