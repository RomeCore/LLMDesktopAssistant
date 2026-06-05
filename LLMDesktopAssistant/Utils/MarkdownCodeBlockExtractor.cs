using System;
using System.Collections.Generic;
using System.Text;
using RCParsing;

namespace LLMDesktopAssistant.Utils
{
	public static class MarkdownCodeBlockExtractor
	{
		private static readonly Parser _codeBlockParser;

		static MarkdownCodeBlockExtractor()
		{
			var builder = new ParserBuilder();

			builder.CreateMainRule()
				.TextUntil("```")
				.Literal("```")
				.TextUntil('\n', '\r')
				.TextUntil("```")
				.Literal("```")
				
				.Transform(v =>
				{
					return v[3].Text.Trim();
				});

			_codeBlockParser = builder.Build();
		}

		public static string TryExtract(string markdownContent)
		{
			if (string.IsNullOrWhiteSpace(markdownContent))
				return markdownContent;
			if (_codeBlockParser.TryParse(markdownContent, out string result))
				return result;
			return markdownContent;
		}
	}
}