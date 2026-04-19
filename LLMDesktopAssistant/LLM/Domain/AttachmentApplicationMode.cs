namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents the mode for how attachments are applied in the user message.
	/// </summary>
	public enum AttachmentApplicationMode
	{
		/// <summary>
		/// Only reference the attachment without loading its contents.
		/// LLM will be provided with only link to '.llmassist/attachments/yyyy-MM-dd-{filename}' file.
		/// </summary>
		OnlyReference,

		/// <summary>
		/// Load the full contents of the attachment with link applied.
		/// </summary>
		FullContents,

		/// <summary>
		/// Load only a partial contents of the attachment with link applied.
		/// </summary>
		PartialContents,

		/// <summary>
		/// Load the full contents of the attachment in hexadecimal format with link applied.
		/// </summary>
		FullHexadecimal,

		/// <summary>
		/// Load only a partial contents of the attachment in hexadecimal format with link applied.
		/// </summary>
		HexadecimalPartial,

		/// <summary>
		/// Load the description of the attachment with link applied.
		/// Description contains information about the attachment (e.g. what image contains, etc.).
		/// </summary>
		Description
	}
}