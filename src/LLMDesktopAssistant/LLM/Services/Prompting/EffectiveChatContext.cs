using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <summary>
	/// The effective chat context of an agent: what messages the agent can see and
	/// which checkpoints are applied to that message sequence.
	/// </summary>
	/// <param name="Messages">The effective message sequence (ascending), including the pending assistant response.</param>
	/// <param name="Checkpoints">Active checkpoints carried by the effective region with their relative indices.</param>
	/// <param name="LastCutIndex">
	/// The cut boundary of the newest cut checkpoint expressed relative to <see cref="Messages"/>:
	/// all messages with index <c>&lt;= LastCutIndex</c> lie before the cut boundary
	/// (i.e. <c>Messages[LastCutIndex + 1]</c> is the first message after the newest cut).
	/// <c>-1</c> when there are no cuts, or when the cut leaves no visible message before it.
	/// The cut-ness logic (which checkpoint kinds are cuts) is computed by the effective context builder only.
	/// </param>
	public record EffectiveChatContext(
		IReadOnlyList<BranchedMessage> Messages,
		IReadOnlyList<EffectiveCheckpoint> Checkpoints,
		int LastCutIndex);
}
