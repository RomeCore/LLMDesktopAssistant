using LLTSharp;

namespace LLMDesktopAssistant.Prompting
{
	/// <summary>
	/// Enumerates the different types of text templates that can be used in prompts.
	/// </summary>
	public enum SerializableTemplateType
	{
		/// <summary>
		/// The template is just plain text, without any special formatting or variables.
		/// </summary>
		PlainText,

		/// <summary>
		/// The template was parsed from LLT syntax as a text template.
		/// </summary>
		LLTText,

		/// <summary>
		/// The template was parsed from LLT syntax as messages template.
		/// </summary>
		LLTMessages,

		/// <summary>
		/// The template was created from <see cref="ITemplate"/> directly, meaning it is not meant for serialization.
		/// </summary>
		Imported
	}
}