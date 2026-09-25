using LLMDesktopAssistant.LLM.Services.Prompting;

namespace LLMDesktopAssistant.Prompting.Context
{
	/// <summary>
	/// Base class for live-tail contexts: delegates all work to the two typed services:
	/// state provider and state renderer.
	/// </summary>
	public abstract class PromptLiveTailContextBase : IPromptLiveTailContextProvider
	{
		/// <inheritdoc/>
		public abstract string Provide(EffectiveChatContext context);
	}
}
