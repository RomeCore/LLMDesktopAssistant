using LLTSharp;

namespace LLMDesktopAssistant.Prompting.Management
{
	public interface IChatTemplateImporter
	{
		TemplateLibrary Library { get; }
	}
}
