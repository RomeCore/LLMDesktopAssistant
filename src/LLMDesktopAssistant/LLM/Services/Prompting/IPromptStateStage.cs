using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Prompting.State;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <summary>
	/// The SCM stage: manages the prompt state anchors of an agent (hybrid prompt mode only).
	/// Called by the prompt composer between the effective messages provider and message conversion.
	/// </summary>
	public interface IPromptStateStage
	{
		/// <summary>
		/// Returns the live anchor of the agent for the given effective context,
		/// creating or rebaselining it when needed.
		/// </summary>
		/// <param name="agent">The agent to process.</param>
		/// <param name="effective">The effective chat context of the agent.</param>
		/// <param name="pendingResponse">The pending assistant response (reserved for state deltas).</param>
		/// <returns>The active anchor, or null outside of hybrid mode.</returns>
		PromptStateAnchorMessageData? Process(ChatAgentDescriptor agent, EffectiveChatContext effective,
			BranchedMessage? pendingResponse);
	}
}
