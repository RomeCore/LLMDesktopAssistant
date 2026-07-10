using LiveMarkdown.Avalonia;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Desktop.Services
{
	/// <summary>
	/// The dummy service that just adds Math nodes to the markdown renderer.
	/// </summary>
	[Service]
	public class MarkdownMermaidExtensionService
	{
		public MarkdownMermaidExtensionService()
		{
			MarkdownRenderer.ConfigurePipeline += x => x.UseMermaid();
			MarkdownNode.Register<MermaidBlockNode>();
		}
	}
}