using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <summary>
	/// The effective chat context of an agent: what messages the agent can see and
	/// which checkpoints are applied to that message sequence.
	/// </summary>
	public class EffectiveChatContext
	{
		/// <summary>
		/// The effective message sequence (ascending), including the pending assistant response.
		/// </summary>
		public IReadOnlyList<BranchedMessage> Messages { get; }

		/// <summary>
		/// Active checkpoints carried by the effective region with their relative indices.
		/// </summary>
		public IReadOnlyList<EffectiveCheckpoint> Checkpoints { get; }

		/// <summary>
		/// The index of the first message in the effective history, relative to the <c>Chat.Messages</c>.
		/// </summary>
		public int EffectiveMessagesStartIndex { get; }

		/// <summary>
		/// The cut boundary of the newest cut checkpoint expressed relative to <see cref="Messages"/>:
		/// all messages with index <c>&lt;= LastCutIndex</c> lie before the cut boundary
		/// (i.e. <c>Messages[LastCutIndex + 1]</c> is the first message after the newest cut).
		/// <c>-1</c> when there are no cuts, or when the cut leaves no visible message before it.
		/// The cut-ness logic (which checkpoint kinds are cuts) is computed by the effective context builder only.
		/// </summary>
		public int LastCutIndex { get; }

		/// <summary>
		/// The index of the last checkpoint in the <see cref="Messages"/> list.
		/// This is useful for determining the most recent checkpoint that has been applied to the context.
		/// The default value is <c>-1</c> if there are no checkpoints.
		/// </summary>
		public int LastCheckpointIndex { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="EffectiveChatContext"/> class.
		/// </summary>
		/// <param name="messages">The effective message sequence (ascending), including the pending assistant response.</param>
		/// <param name="checkpoints">Active checkpoints carried by the effective region with their relative indices.</param>
		/// <param name="effectiveMessagesStartIndex">The index of the first message in the effective history, relative to the <c>Chat.Messages</c>.</param>
		/// <param name="lastCutIndex">The cut boundary of the newest cut checkpoint expressed relative to <see cref="Messages"/>. The cut-ness logic (which checkpoint kinds are cuts) is computed by the effective context builder only. The default value is <c>-1</c>.</param>
		/// <param name="lastCheckpointIndex">The index of the last checkpoint in the <see cref="Messages"/> list. The default value is <c>-1</c>.</param>
		public EffectiveChatContext(
			IReadOnlyList<BranchedMessage> messages,
			IReadOnlyList<EffectiveCheckpoint> checkpoints,
			int effectiveMessagesStartIndex, int lastCutIndex, int lastCheckpointIndex)
		{
			Messages = messages;
			Checkpoints = checkpoints;
			EffectiveMessagesStartIndex = effectiveMessagesStartIndex;
			LastCutIndex = lastCutIndex;
			LastCheckpointIndex = lastCheckpointIndex;
		}
	}
}
