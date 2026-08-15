using LiveMarkdown.Avalonia;
using LLMDesktopAssistant.Markdown.Parsers;
using LLMDesktopAssistant.Markdown.UINodes;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Markdown;

/// <summary>
/// Configures the Markdown pipeline with custom extensions
/// for quick actions, quick explanations, and other LLM-specific syntax.
/// </summary>
[Service]
public class MarkdownAdditionalNodesConfigurator
{
	public MarkdownAdditionalNodesConfigurator()
	{
		MarkdownRenderer.ConfigurePipeline += ConfigurePipeline;

		MarkdownNode.Register<QuickActionUiNode>();
		MarkdownNode.Register<QuickExplanationUiNode>();
	}

	private static void ConfigurePipeline(Markdig.MarkdownPipelineBuilder pipeline)
	{
		var linkParserIndex = pipeline.InlineParsers.FindIndex(p => p is Markdig.Parsers.Inlines.LinkInlineParser);
		if (linkParserIndex >= 0)
			pipeline.InlineParsers.Insert(linkParserIndex, new QuickActionParser());
		else
			pipeline.InlineParsers.Add(new QuickActionParser());

		if (linkParserIndex >= 0)
			pipeline.InlineParsers.Insert(linkParserIndex, new QuickExplanationParser());
		else
			pipeline.InlineParsers.Add(new QuickExplanationParser());
	}
}
