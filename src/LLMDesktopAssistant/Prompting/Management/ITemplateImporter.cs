using LLMDesktopAssistant.Utils;
using LLTSharp;

namespace LLMDesktopAssistant.Prompting.Management
{
	public interface ITemplateImporter
	{
		TemplateLibrary Library { get; }

		ReadOnlyObservableCollection<ITemplate> Templates { get; }
	}
}
