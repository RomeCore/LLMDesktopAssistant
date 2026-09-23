using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Prompting;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <summary>
	/// The set of compaction render rules applying to a single effective message.
	/// </summary>
	public readonly record struct MessageCompaction(bool CompactTools, bool ForceCompactTools, bool CompactReasoning)
	{
		/// <summary>
		/// Whether any compaction applies to the message.
		/// </summary>
		public bool Any => CompactTools || ForceCompactTools || CompactReasoning;

		/// <summary>
		/// Whether the result of a tool call should be replaced with a placeholder.
		/// </summary>
		public bool ShouldCompactToolCall(bool canBeCompacted) => ForceCompactTools || (CompactTools && canBeCompacted);

		/// <summary>
		/// Builds the compaction rules for the effective message at the given index
		/// (a checkpoint covers every message with an effective index not greater than its own,
		/// so its carrier message is covered too). The agent-level disabled flags cut off kinds.
		/// </summary>
		public static MessageCompaction ForMessage(IReadOnlyList<EffectiveCheckpoint> checkpoints, int messageIndex,
			ContextCheckpointKind disabledFlags)
		{
			bool compactTools = false, forceCompactTools = false, compactReasoning = false;

			foreach (var checkpoint in checkpoints)
			{
				if (messageIndex > checkpoint.Index)
					continue;

				var kind = checkpoint.Checkpoint.Kind & ~disabledFlags;

				if (kind.HasFlag(ContextCheckpointKind.ToolCompaction))
					compactTools = true;
				if (kind.HasFlag(ContextCheckpointKind.ForcedToolCompaction))
					forceCompactTools = true;
				if (kind.HasFlag(ContextCheckpointKind.ReasoningCompaction))
					compactReasoning = true;
			}

			return new MessageCompaction(compactTools, forceCompactTools, compactReasoning);
		}

		/// <summary>
		/// The placeholder that replaces a compacted tool result (the previous status is preserved).
		/// </summary>
		public static string GetCompactedToolResultContent(ToolStatus status) => status switch
		{
			ToolStatus.Success => "[TOOL RESULT WAS COMPACTED, THE TOOL EXECUTED WAS SUCCESSFUL BEFORE]",
			ToolStatus.Error => "[TOOL RESULT WAS COMPACTED, THE TOOL EXECUTED WAS FAULTED BEFORE]",
			ToolStatus.Cancelled => "[TOOL RESULT WAS COMPACTED, THE TOOL EXECUTION WAS CANCELLED BEFORE]",
			_ => "[TOOL RESULT WAS COMPACTED]"
		};
	}
}
