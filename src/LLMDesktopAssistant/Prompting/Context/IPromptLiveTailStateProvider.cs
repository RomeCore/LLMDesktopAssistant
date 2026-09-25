using LLMDesktopAssistant.LLM.Services.Prompting;

namespace LLMDesktopAssistant.Prompting.Context
{
	/// <summary>
	/// Captures the live-tail state of a section for the current effective chat context.
	/// </summary>
	public interface IPromptLiveTailStateProvider
	{
		/// <summary>
		/// Captures the current state, or null when the section has nothing to provide for this request.
		/// </summary>
		/// <param name="context">The effective chat context to consider when capturing the state.</param>
		string? CaptureState(EffectiveChatContext context);
	}
}
