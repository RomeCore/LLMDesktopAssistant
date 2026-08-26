using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Prompting.Management;
using LLTSharp;

namespace LLMDesktopAssistant.Prompting
{
	[ChatService(typeof(ITemplateLibraryAccessor))]
	public class ChatTemplateLibraryAccessor(
		IChatTemplateImporter templateImporter
	) : TemplateLibraryAccessorBase
	{
		public override TemplateLibrary Library => templateImporter.Library;
	}
}
