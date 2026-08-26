using LLTSharp;
using LLTSharp.Metadata;

namespace LLMDesktopAssistant.Prompting
{
	public interface ITemplateLibraryAccessor
	{
		ITemplate GetTemplate(string id, params IMetadata[] metadata);
		IMessagesTemplate GetMessagesTemplate(string id, params IMetadata[] metadata);
		ITextTemplate GetTextTemplate(string id, params IMetadata[] metadata);
	}
}