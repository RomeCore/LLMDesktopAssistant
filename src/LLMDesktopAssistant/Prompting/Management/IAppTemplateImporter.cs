using LLTSharp;

namespace LLMDesktopAssistant.Prompting.Management
{
	public interface IAppTemplateImporter : ITemplateImporter
	{
		IEnumerable<ITemplate> BuiltInTemplates { get; }
	}
}
