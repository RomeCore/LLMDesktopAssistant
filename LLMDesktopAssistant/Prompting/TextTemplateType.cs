namespace LLMDesktopAssistant.Prompting
{
	/// <summary>
	/// Enumerates the different types of text templates that can be used in prompts.
	/// </summary>
	public enum TextTemplateType
	{
		/// <summary>
		/// The template is just plain text, without any special formatting or variables.
		/// </summary>
		PlainText,

		/// <summary>
		/// The template was parsed from LLT syntax.
		/// </summary>
		LLT
	}
}