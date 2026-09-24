using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Services.Prompting;

namespace LLMDesktopAssistant.Prompting.State
{
	/// <summary>
	/// A prompt section: a part of the system prompt that captures its own state
	/// and renders it into a text fragment and/or tool definitions.
	/// </summary>
	public interface IPromptSection
	{
		/// <summary>
		/// The type of the state captured by this section.
		/// </summary>
		Type StateType { get; }

		/// <summary>
		/// The type of the delta produced by this section.
		/// </summary>
		Type DeltaType { get; }

		/// <summary>
		/// The merge order of this section.
		/// </summary>
		int Order { get; }

		/// <summary>
		/// Captures the current state of this section for the given agent.
		/// </summary>
		PromptSectionStateBase? CaptureState(ChatAgentDescriptor agent);

		/// <summary>
		/// Calculates the delta between the recorded state (anchor + deltas) and the actual state.
		/// </summary>
		/// <returns>The calculated delta, or null if no changes were detected.</returns>
		PromptSectionDeltaBase? CalculateDelta(PromptSectionStateBase? anchorState,
			IEnumerable<PromptSectionDeltaBase> existingDeltas, PromptSectionStateBase? actualState,
			EffectiveChatContext context);

		/// <summary>
		/// Renders the captured state into a system prompt snapshot fragment (text and/or tools).
		/// </summary>
		SystemPromptSnapshot RenderState(PromptSectionStateBase state);

		/// <summary>
		/// Renders a delta into a compact text fragment.
		/// </summary>
		string RenderDelta(PromptSectionDeltaBase delta);
	}
}
