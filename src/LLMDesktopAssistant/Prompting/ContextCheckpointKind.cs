using System;

namespace LLMDesktopAssistant.Prompting
{
	/// <summary>
	/// Kinds of context checkpoints. Flags: the same mask is used to disable checkpoint kinds per agent.
	/// </summary>
	[Flags]
	public enum ContextCheckpointKind
	{
		/// <summary>No checkpoint kinds.</summary>
		None = 0,

		/// <summary>
		/// The context checkpoint just cuts off the conversation history until the applied message (and including it).
		/// </summary>
		Shield = 1 << 0,

		/// <summary>
		/// The context checkpoint cuts off history (like <see cref="Shield"/>) but also provides an agent-readable summary of the cut-off history.
		/// </summary>
		Summary = 1 << 1,

		/// <summary>
		/// The context checkpoint compacts tool call results until the applied message (and including it).
		/// </summary>
		ToolCompaction = 1 << 2,

		/// <summary>
		/// Same as <see cref="ToolCompaction"/> but ignores per-call compactibility and compacts everything.
		/// </summary>
		ForcedToolCompaction = 1 << 3,

		/// <summary>
		/// The context checkpoint removes reasoning content until the applied message (and including it).
		/// </summary>
		ReasoningCompaction = 1 << 4,
	}
}
