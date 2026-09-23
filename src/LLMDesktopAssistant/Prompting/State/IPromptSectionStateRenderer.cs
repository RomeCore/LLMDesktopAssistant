namespace LLMDesktopAssistant.Prompting.State
{
	public interface IPromptSectionStateRenderer<TState>
		where TState : PromptSectionStateBase
	{
		SystemPromptSnapshot Render(TState state);
	}
}
