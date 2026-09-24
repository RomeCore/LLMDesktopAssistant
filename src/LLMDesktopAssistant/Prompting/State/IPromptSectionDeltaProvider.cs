using LLMDesktopAssistant.LLM.Services.Prompting;

namespace LLMDesktopAssistant.Prompting.State
{
	public interface IPromptSectionDeltaProvider<TState, TDelta>
		where TState : PromptSectionStateBase
		where TDelta : PromptSectionDeltaBase
	{
		/// <summary>
		/// Calculates the delta between recorded state (anchor + deltas) and the actual state.
		/// </summary>
		/// <param name="anchorState">The anchor state to compare against. This is the state before any deltas were applied.</param>
		/// <param name="existingDeltas">The existing deltas to apply to the anchor state to get the current state.</param>
		/// <param name="actualState">The actual state to compare against.</param>
		/// 
		/// <returns>The calculated delta, or null if no changes were detected.</returns>
		TDelta? CalculateDelta(TState? anchorState, IEnumerable<TDelta> existingDeltas,
			TState? actualState, EffectiveChatContext context);
	}
}
