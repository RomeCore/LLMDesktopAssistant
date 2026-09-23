namespace LLMDesktopAssistant.Prompting.State
{
	public interface IPromptSectionDeltaRenderer<TDelta>
		where TDelta : PromptSectionDeltaBase
	{
		string Render(TDelta delta);
	}
}
