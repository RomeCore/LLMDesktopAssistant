using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ganss.Xss;
using RCParsing;

namespace LLMDesktopAssistant.Utils.Web
{
	public static class HtmlSanitizer
	{
		private static readonly Ganss.Xss.HtmlSanitizer _sanitizer;
		private static readonly Parser _postSanitizer;

		static HtmlSanitizer()
		{
			_sanitizer = new Ganss.Xss.HtmlSanitizer();
			_sanitizer.AllowedTags.Remove("script");

			var postSanitizerBuilder = new ParserBuilder();

			postSanitizerBuilder.CreateMainRule()
				.Chars(char.IsWhiteSpace, min: 2)
					.Transform(v =>
					{
						if (v.Span.ContainsAny("\r\n"))
							return "\n";
						return " ";
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