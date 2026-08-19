namespace LLMDesktopAssistant.Tools.Meta
{
	/// <summary>
	/// The diagnostic codes that describe problems found while scanning and parsing meta tools.
	/// </summary>
	[Flags]
	public enum MetaToolDiagnosticCode
	{
		/// <summary>
		/// No problems detected.
		/// </summary>
		None = 0,

		/// <summary>
		/// The file does not contain a frontmatter block.
		/// </summary>
		MissingFrontmatter = 1 << 0,

		/// <summary>
		/// The frontmatter block cannot be parsed.
		/// </summary>
		FrontmatterParsingError = 1 << 1,

		/// <summary>
		/// The frontmatter block cannot be decoded into the expected structure.
		/// </summary>
		FrontmatterDecodingError = 1 << 2,

		/// <summary>
		/// The file name is not a valid tool name.
		/// </summary>
		NameFormatError = 1 << 3,

		/// <summary>
		/// The approval level value is not recognized.
		/// </summary>
		InvalidApprovalLevel = 1 << 4,

		/// <summary>
		/// The behaviours list contains unrecognized values.
		/// </summary>
		InvalidBehaviours = 1 << 5,

		/// <summary>
		/// The argument schema is not a valid JSON.
		/// </summary>
		InvalidArgumentSchema = 1 << 6,

		/// <summary>
		/// The tool file does not exist.
		/// </summary>
		MissingFile = 1 << 7,

		/// <summary>
		/// The tool file cannot be accessed (read error).
		/// </summary>
		FileAccessError = 1 << 8,

		/// <summary>
		/// A general parsing error occurred.
		/// </summary>
		GeneralParsingError = 1 << 9
	}
}
