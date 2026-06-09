namespace LLMDesktopAssistant.Utils.Files
{
	/// <summary>
	/// Represents the hunk diff line, computed by <see cref="UnifiedDiff"/>.
	/// </summary>
	public readonly struct HunkLine
	{
		/// <summary>
		/// The kind of line: '+' for addition, '-' for deletion, ' ' for context.
		/// </summary>
		public required readonly char Kind { get; init; }

		/// <summary>
		/// The content of the line, excluding the kind character.
		/// </summary>
		public required readonly string Content { get; init; }

		/// <summary>
		/// Returns a string representation of the line, suitable for use in a unified diff.
		/// The output format (unified diff):
		/// <code>
		/// {Kind}{Content}
		/// </code>
		/// Examples:
		/// <code>
		///  Context line
		/// -Removed line
		/// +Added line
		/// </code>
		/// </summary>
		public readonly override string ToString() => $"{Kind}{Content}";
	}
}