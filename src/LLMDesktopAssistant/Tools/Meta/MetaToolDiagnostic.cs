namespace LLMDesktopAssistant.Tools.Meta
{
	/// <summary>
	/// The diagnostic containing specific warnings and errors that occurred while scanning
	/// or parsing a meta tool.
	/// </summary>
	public class MetaToolDiagnostic
	{
		/// <summary>
		/// Gets a value indicating whether the tool cannot be used at all.
		/// </summary>
		public required bool IsFatal { get; init; }

		/// <summary>
		/// Gets the diagnostic codes describing the detected problems.
		/// </summary>
		public required MetaToolDiagnosticCode Codes { get; init; }

		/// <summary>
		/// Gets the exception that caused the failure, if any.
		/// </summary>
		public required Exception? Exception { get; init; }
	}
}
