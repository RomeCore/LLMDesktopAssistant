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
	/// the effective index of the carrier message, or the index of the nearest preceding visible
	/// message when the carrier is invisible, or <c>-1</c> when there is no preceding visible message.
	/// The same rule applies to cut checkpoints (shield/summary): a cut that leaves no visible
	/// message before it is carried at <c>-1</c>.
	/// </param>
	public record EffectiveCheckpoint(ContextCheckpoint Checkpoint, int Index);
}
