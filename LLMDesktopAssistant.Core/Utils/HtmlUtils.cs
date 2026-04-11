using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ganss.Xss;
using RCParsing;

namespace LLMDesktopAssistant.Core.Utils
{
	public static class HtmlUtils
	{
		private static readonly HtmlSanitizer _sanitizer;
		private static readonly Parser _postSanitizer;

		static HtmlUtils()
		{
			_sanitizer = new HtmlSanitizer();
			_sanitizer.AllowedTags.Remove("script");

			var postSanitizerBuilder = new ParserBuilder();

			postSanitizerBuilder.CreateMainRule()
				.Chars(char.IsWhiteSpace, min: 2)
					.Transform(v =>
					{
						if (v.Span.ContainsAny("\r\n"))
							return "\n";
						return "";
					});

			_postSanitizer = postSanitizerBuilder.Build();
		}

		public static string Sanitize(string html)
		{
			html = _sanitizer.Sanitize(html);
			html = _postSanitizer.ReplaceAllMatches(html);

			return html;
		}
	}
}