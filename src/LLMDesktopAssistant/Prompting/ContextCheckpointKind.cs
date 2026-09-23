namespace LLMDesktopAssistant.Prompting
{
	public enum ContextCheckpointKind
	{
		/// <summary>
		/// The context checkpoint just cuts off the conversation history until the applied message (and including it).
		/// </summary>
		Shield,

		/// <summary>
		/// The context checkpoint cuts off history (like <see cref="Shield"/>) but also provides an agent-readable summary of the cut-off history.
		/// </summary>
		Summary,

		/// <summary>
		/// The context checkpoint compacts tool calls until the applied message (and including it).
		/// </summary>
		ToolCompact,
	}
}
