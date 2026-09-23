using LLMDesktopAssistant.Prompting;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <summary>
	/// A checkpoint that is active for the effective chat context of an agent,
	/// together with its position relative to <see cref="EffectiveChatContext.Messages"/>.
	/// </summary>
	/// <param name="Checkpoint">The underlying chat data checkpoint.</param>
	/// <param name="Index">
	/// Position relative to <see cref="EffectiveChatContext.Messages"/>:
	/// <c>-1</c> for cut checkpoints (shield/summary);
	/// for non-cut checkpoints — the effective index of the carrier message,
	/// or the index of the nearest preceding visible message when the carrier is invisible,
	/// or <c>-1</c> when there is no preceding visible message.
	/// </param>
	public record EffectiveCheckpoint(ContextCheckpoint Checkpoint, int Index);
}
