namespace LLMDesktopAssistant.Prompting.Context
{
	public interface IPromptSectionDeltaRenderer<TDelta>
		where TDelta : PromptSectionDeltaBase
	{
		string Render(TDelta delta);
	}
}
