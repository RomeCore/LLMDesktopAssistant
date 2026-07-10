using LiveMarkdown.Avalonia;
using LLMDesktopAssistant.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Desktop.Services
{
	/// <summary>
	/// The dummy service that just adds Math nodes to the markdown renderer.
	/// </summary>
	[Service]
	public class MarkdownMathExtensionService
	{
		public MarkdownMathExtensionService()
		{
			// TODO: This can do some rendering errors, uncomment when fixed
			// MarkdownRenderer.ConfigurePipeline += x => x.UseExtendedMathematics();
			MarkdownNode.Register<MathInlineNode>();
			MarkdownNode.Register<MathBlockNode>();
		}
	}
}