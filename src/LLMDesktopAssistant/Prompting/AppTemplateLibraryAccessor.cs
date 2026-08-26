using LLMDesktopAssistant.Prompting.Management;
using LLMDesktopAssistant.Services;
using LLTSharp;

namespace LLMDesktopAssistant.Prompting
{
	[Service(typeof(ITemplateLibraryAccessor))]
	public class AppTemplateLibraryAccessor(
		IAppTemplateImporter templateImporter
	) : TemplateLibraryAccessorBase
	{
		public override TemplateLibrary Library => templateImporter.Library;
	}
}
