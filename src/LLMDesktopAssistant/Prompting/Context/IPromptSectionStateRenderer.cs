namespace LLMDesktopAssistant.Prompting.Context
{
	public interface IPromptSectionStateRenderer<TState>
		where TState : PromptSectionStateBase
	{
		SystemPromptSnapshot Render(TState state);
	}
}
