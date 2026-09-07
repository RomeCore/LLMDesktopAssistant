using LLTSharp;

namespace LLMDesktopAssistant.Prompting.Management
{
	public interface ITemplateParser
	{
		/// <summary>
		/// Gets the supported file extensions for this parser without the leading dot.
		/// </summary>
		string[] SupportedExtensions { get; }

		/// <summary>
		/// Parses the given content with the specified extension and returns a collection of templates.
		/// </summary>
		/// <param name="content">The textual content to parse.</param>
		/// <param name="extension">The file extension without the leading dot.</param>
		IEnumerable<ITemplate> Parse(string content, string extension);
	}
}
