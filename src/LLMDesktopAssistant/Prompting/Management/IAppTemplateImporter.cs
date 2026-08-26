using LLMDesktopAssistant.Utils;
using LLTSharp;

namespace LLMDesktopAssistant.Prompting.Management
{
	public interface IAppTemplateImporter
	{
		TemplateLibrary Library { get; }

		IEnumerable<ITemplate> BuiltInTemplates { get; }

		ReadOnlyObservableCollection<ITemplate> UserTemplates { get; }

		ReadOnlyObservableCollection<(string Path, Exception Exception)> ImportingErrors { get; }
	}
}
