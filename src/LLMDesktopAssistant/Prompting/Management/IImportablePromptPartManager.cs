using LLTSharp;

namespace LLMDesktopAssistant.Prompting.Management
{
	public interface IImportablePromptPartManager
	{
		string TemplateType { get; }

		/// <summary>
		/// Imports prompt part from a template.
		/// </summary>
		/// <param name="template">The template to import prompt part from.</param>
		/// <param name="source">The source of the template.</param>
		/// <returns>True if the prompt part was successfully imported and not present before, false otherwise.</returns>
		bool ImportFromTemplate(ITemplate template, PromptPartSource source);

		/// <summary>
		/// Removes registered prompt part that associated with the specified template.
		/// </summary>
		/// <param name="template">The template to remove associated prompt part.</param>
		/// <returns>True if any prompt part was removed, false otherwise.</returns>
		bool DropTemplate(ITemplate template);
	}
}
