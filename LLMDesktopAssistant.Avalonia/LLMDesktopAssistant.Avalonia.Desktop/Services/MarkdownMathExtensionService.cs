using LiveMarkdown.Avalonia;
using LLMDesktopAssistant.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Avalonia.Desktop.Services
{
	[Service]
	public class MarkdownMathExtensionService
	{
		public MarkdownMathExtensionService()
		{
			MarkdownNode.Register<MathInlineNode>();
			MarkdownNode.Register<MathBlockNode>();
		}
	}
}