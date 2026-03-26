using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using Emoji.Wpf;
using LLMDesktopAssistant.Utils.Markdown;
using Markdig;
using Markdig.Renderers;
using Markdig.Wpf;

namespace LLMDesktopAssistant.Utils
{
	public static class MarkdownParser
	{
		private static readonly MarkdownPipeline _pipeline;

		static MarkdownParser()
		{
			_pipeline = new MarkdownPipelineBuilder()
				.UseSupportedExtensions()
				.UseEmojiAndSmiley(enableSmileys: false)
				.UseMathematics()
				.Build();
		}

		private static WpfRenderer CreateWpfRenderer()
		{
			var renderer = new WpfRenderer();

			renderer.ObjectRenderers.RemoveAll(r =>
				r is Markdig.Renderers.Wpf.Inlines.LinkInlineRenderer);

			renderer.ObjectRenderers.Add(new LinkInlineRenderer());
			renderer.ObjectRenderers.Add(new MathInlineRenderer());
			renderer.ObjectRenderers.Add(new MathBlockRenderer());

			return renderer;
		}

		private static HtmlRenderer CreatePTRenderer(TextWriter writer)
		{
			var renderer = new HtmlRenderer(writer)
			{
				EnableHtmlForBlock = false,
				EnableHtmlForInline = false,
				EnableHtmlEscape = false,
			};

			return renderer;
		}

		private static HtmlRenderer CreateSpeechablePTRenderer(TextWriter writer)
		{
			var renderer = new HtmlRenderer(writer)
			{
				EnableHtmlForBlock = false,
				EnableHtmlForInline = false,
				EnableHtmlEscape = false,
			};
			renderer.ObjectRenderers.RemoveAll(r =>
				r is Markdig.Renderers.Html.CodeBlockRenderer ||
				r is Markdig.Renderers.Html.Inlines.CodeInlineRenderer ||
				r is Markdig.Renderers.Html.Inlines.LineBreakInlineRenderer);

			return renderer;
		}

		/// <summary>
		/// Parses the specified markdown into a <see cref="FlowDocument"/>.
		/// </summary>
		/// <param name="markdown">The markdown to parse.</param>
		/// <returns>A <see cref="FlowDocument"/> containing the parsed markdown.</returns>
		public static FlowDocument ParseDocument(string markdown)
		{
			var document = new FlowDocument { PagePadding = new Thickness(0) };
			document.SetResourceReference(FrameworkContentElement.StyleProperty, Styles.DocumentStyleKey);

			var mdDocument = Markdig.Markdown.Parse(markdown, _pipeline);
			var renderer = CreateWpfRenderer();

			renderer.LoadDocument(document);
			_pipeline.Setup(renderer);
			renderer.Render(mdDocument);

			document.SubstituteGlyphs();
			return document;
		}

		/// <summary>
		/// Parses markdown into plain text.
		/// </summary>
		/// <param name="text">The markdown text to parse.</param>
		/// <returns>The plain text.</returns>
		public static string ParsePlainText(string text)
		{
			var mdDocument = Markdig.Markdown.Parse(text, _pipeline);

			var writer = new StringWriter();
			var renderer = CreatePTRenderer(writer);
			_pipeline.Setup(renderer);
			renderer.Render(mdDocument);
			writer.Flush();

			return writer.ToString();
		}

		/// <summary>
		/// Parses markdown into plain text.
		/// </summary>
		/// <param name="text">The markdown text to parse.</param>
		/// <returns>The plain text.</returns>
		public static string ParseSpeechablePlainText(string text)
		{
			var mdDocument = Markdig.Markdown.Parse(text, _pipeline);

			var writer = new StringWriter();
			var renderer = CreateSpeechablePTRenderer(writer);
			_pipeline.Setup(renderer);
			renderer.Render(mdDocument);
			writer.Flush();

			return writer.ToString();
		}

		/// <summary>
		/// Parses markdown into a pair of two <see cref="FlowDocument"/>s: one for the completed document and another for the incomplete portion.
		/// </summary>
		/// <param name="completedDocument">The completed document.</param>
		/// <param name="incompletedDocument">The incomplete portion.</param>
		/// <param name="oldText">The old text.</param>
		/// <param name="newText">The new text.</param>
		/// <param name="accumulatedMarkdownLength">The length of the accumulated markdown.</param>
		public static void ParseDocumentStreaming(FlowDocument completedDocument, FlowDocument incompletedDocument,
			string oldText, string newText,
			ref int accumulatedMarkdownLength)
		{
			// Check if the new text starts with the old text
			if (!newText.StartsWith(oldText))
			{
				completedDocument.Blocks.Clear();
				accumulatedMarkdownLength = 0;
			}
			if (accumulatedMarkdownLength > newText.Length)
			{
				accumulatedMarkdownLength = newText.Length;
			}

			string markdownText = newText.Substring(accumulatedMarkdownLength);
			var mdDocument = Markdig.Markdown.Parse(markdownText, _pipeline);
			var renderer = CreateWpfRenderer();

			incompletedDocument.Blocks.Clear();
			completedDocument.SetResourceReference(FrameworkContentElement.StyleProperty, Styles.DocumentStyleKey);
			incompletedDocument.SetResourceReference(FrameworkContentElement.StyleProperty, Styles.DocumentStyleKey);

			renderer.LoadDocument(incompletedDocument);
			_pipeline.Setup(renderer);
			renderer.Render(mdDocument);

			// We are saving the first complete blocks and removing them from the markdown text
			if (mdDocument.Count > 1)
			{
				var lastBlock = mdDocument[mdDocument.Count - 1];
				int incompleteStart = lastBlock.Span.Start;

				var completeBlocks = incompletedDocument.Blocks
					.Cast<Block>()
					.Take(mdDocument.Count - 1)
					.ToList();

				foreach (var block in completeBlocks)
				{
					incompletedDocument.Blocks.Remove(block);
					completedDocument.Blocks.Add(block);
				}

				var lastCompleteBlock = completeBlocks.Last();
				completedDocument.PagePadding = new Thickness(0, 0, 0, lastCompleteBlock.Margin.Bottom);
				completedDocument.SubstituteGlyphs();

				accumulatedMarkdownLength += incompleteStart;
			}

			incompletedDocument.SubstituteGlyphs();
		}

		/// <summary>
		/// Parses markdown into a completed speechable plaintext blocks.
		/// </summary>
		/// <param name="oldText">The old text.</param>
		/// <param name="newText">The new text.</param>
		/// <param name="accumulatedMarkdownLength">The length of the accumulated markdown.</param>
		/// <returns>The speechable plaintext blocks. May be null if there are no blocks.</returns>
		public static string? ParseSpeechablePlainTextStreaming(string oldText, string newText,
			ref int accumulatedMarkdownLength)
		{
			// Check if the new text starts with the old text
			if (!newText.StartsWith(oldText))
			{
				accumulatedMarkdownLength = 0;
			}
			if (accumulatedMarkdownLength > newText.Length)
			{
				accumulatedMarkdownLength = newText.Length;
			}

			string markdownText = newText.Substring(accumulatedMarkdownLength);
			var mdDocument = Markdig.Markdown.Parse(markdownText, _pipeline);

			if (mdDocument.Count > 1)
			{
				// Remove the last block since it is not complete yet.
				mdDocument.RemoveAt(mdDocument.Count - 1);
				accumulatedMarkdownLength = mdDocument[^1].Span.End;

				var writer = new StringWriter();
				var renderer = CreateSpeechablePTRenderer(writer);
				_pipeline.Setup(renderer);
				renderer.Render(mdDocument);
				writer.Flush();

				return writer.ToString();
			}

			return null;
		}

		/// <summary>
		/// Parses markdown into a completed speechable plaintext blocks.
		/// </summary>
		/// <param name="accumulator">The accumulator for the markdown text.</param>
		/// <returns>The speechable plaintext blocks. May be null if there are no blocks.</returns>
		public static string? ParseSpeechablePlainTextStreaming(StringBuilder accumulator)
		{
			string markdownText = accumulator.ToString();
			var mdDocument = Markdig.Markdown.Parse(markdownText, _pipeline);

			if (mdDocument.Count > 1)
			{
				// Remove the last block since it is not complete yet.
				mdDocument.RemoveAt(mdDocument.Count - 1);
				accumulator.Remove(0, mdDocument.LastChild!.Span.End + 1);

				var writer = new StringWriter();
				var renderer = CreateSpeechablePTRenderer(writer);
				_pipeline.Setup(renderer);
				renderer.Render(mdDocument);
				writer.Flush();

				return writer.ToString();
			}

			return null;
		}
	}
}