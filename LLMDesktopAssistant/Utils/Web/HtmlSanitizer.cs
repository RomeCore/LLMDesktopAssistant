using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using RCParsing;

namespace LLMDesktopAssistant.Utils.Web
{
	/// <summary>
	/// Sanitizes HTML strings for LLM consumption: removes dangerous elements, attributes and URL schemes,
	/// hidden content and HTML comments (common prompt injection vectors), and noisy sections
	/// such as <c>nav</c>, <c>footer</c>, <c>header</c> and <c>noscript</c>.
	/// Parsing and serialization are done with AngleSharp.
	/// </summary>
	public static class HtmlSanitizer
	{
		/// <summary>
		/// Tags whose content is preserved during sanitization. All other tags are removed together with their content.
		/// </summary>
		private static readonly HashSet<string> _allowedTags = new(StringComparer.OrdinalIgnoreCase)
		{
			"a", "abbr", "acronym", "address", "area", "article", "aside", "audio",
			"b", "bdi", "bdo", "big", "blockquote", "body", "br", "button", "canvas",
			"caption", "center", "cite", "code", "col", "colgroup", "data", "datalist",
			"dd", "del", "details", "dfn", "dialog", "div", "dl", "dt", "em",
			"fieldset", "figcaption", "figure", "font", "form", "h1", "h2",
			"h3", "h4", "h5", "h6", "head", "hr", "html", "i", "img",
			"input", "ins", "kbd", "label", "legend", "li", "link", "main", "map",
			"mark", "menu", "meter", "ol", "optgroup", "option", "output", "p",
			"picture", "pre", "progress", "q", "rp", "rt", "ruby", "s", "samp",
			"section", "select", "slot", "small", "source", "span", "strike", "strong",
			"sub", "summary", "sup", "table", "tbody", "td", "textarea", "tfoot", "th",
			"thead", "time", "tr", "track", "u", "ul", "var", "video", "wbr"
		};

		/// <summary>
		/// Attributes that are preserved on allowed tags. Event handlers (<c>on*</c>) are rejected separately.
		/// <c>data-*</c> and <c>aria-*</c> attributes are always allowed.
		/// </summary>
		private static readonly HashSet<string> _allowedAttributes = new(StringComparer.OrdinalIgnoreCase)
		{
			"abbr", "accept", "accept-charset", "accesskey", "action", "align", "alt",
			"as", "async", "autocomplete", "autoplay", "charset", "checked", "cite",
			"class", "color", "cols", "colspan", "content", "contenteditable", "controls",
			"coords", "data", "datetime", "default", "dir", "dirname", "disabled",
			"download", "draggable", "dropzone", "enctype", "for", "form", "formaction",
			"headers", "height", "hidden", "high", "href", "hreflang", "http-equiv",
			"id", "ismap", "itemscope", "itemtype", "kind", "label", "lang", "language",
			"list", "loop", "low", "max", "maxlength", "media", "method", "min",
			"multiple", "muted", "name", "novalidate", "open", "optimum", "pattern",
			"placeholder", "playsinline", "poster", "preload", "pubdate", "radiogroup",
			"readonly", "rel", "required", "reverse", "rows", "rowspan", "sandbox",
			"scope", "selected", "shape", "size", "sizes", "span", "spellcheck",
			"src", "srclang", "start", "step", "style", "tabindex", "target", "title",
			"translate", "type", "usemap", "valign", "value", "width", "wrap"
		};

		/// <summary>
		/// Attributes whose value is treated as a URL and validated against <see cref="_allowedUrlSchemes"/>.
		/// </summary>
		private static readonly HashSet<string> _urlAttributes = new(StringComparer.OrdinalIgnoreCase)
		{
			"action", "cite", "formaction", "href", "poster", "src"
		};

		/// <summary>
		/// URL schemes that are allowed in URL attributes. Relative URLs without a scheme are always allowed.
		/// </summary>
		private static readonly HashSet<string> _allowedUrlSchemes = new(StringComparer.OrdinalIgnoreCase)
		{
			"http", "https", "mailto"
		};

		private static readonly Regex _hiddenStyleRegex = new(
			@"(?:display\s*:\s*none|visibility\s*:\s*(?:hidden|collapse))",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static readonly HtmlParser _parser = new();
		private static readonly Parser _postSanitizer;

		static HtmlSanitizer()
		{
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

		/// <summary>
		/// Removes dangerous tags, attributes and URL values from the specified HTML string
		/// and collapses runs of whitespace.
		/// </summary>
		/// <param name="html">The HTML to sanitize. <see langword="null"/> or empty input produces an empty result.</param>
		/// <returns>The sanitized HTML, or <see cref="string.Empty"/> if <paramref name="html"/> is <see langword="null"/> or empty.</returns>
		public static string Sanitize(string? html)
		{
			if (string.IsNullOrEmpty(html))
				return string.Empty;

			var document = _parser.ParseDocument(html);

			foreach (var element in document.All.ToArray())
			{
				if (!_allowedTags.Contains(element.LocalName) || IsHiddenElement(element))
				{
					element.Remove();
					continue;
				}

				foreach (var attribute in element.Attributes.ToArray())
				{
					if (!IsAllowedAttribute(attribute))
						element.RemoveAttribute(attribute.Name);
				}
			}

			foreach (var comment in document.Descendants<IComment>().ToArray())
				comment.Remove();

			html = document.DocumentElement?.OuterHtml ?? string.Empty;
			return _postSanitizer.ReplaceAllMatches(html);
		}

		private static bool IsHiddenElement(IElement element)
		{
			if (element.HasAttribute("hidden"))
				return true;

			var ariaHidden = element.GetAttribute("aria-hidden");
			if (ariaHidden is not null && ariaHidden.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
				return true;

			var style = element.GetAttribute("style");
			return style is not null && _hiddenStyleRegex.IsMatch(style);
		}


		private static bool IsAllowedAttribute(IAttr attribute)
		{
			var name = attribute.Name;

			if (name.StartsWith("data-", StringComparison.OrdinalIgnoreCase)
				|| name.StartsWith("aria-", StringComparison.OrdinalIgnoreCase))
				return true;

			if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase) && name.Length > 2 && char.IsLetter(name[2]))
				return false;

			if (!_allowedAttributes.Contains(name))
				return false;

			if (_urlAttributes.Contains(name) && !IsSafeUrl(attribute.Value))
				return false;

			if (name.Equals("style", StringComparison.OrdinalIgnoreCase) && !IsSafeStyle(attribute.Value))
				return false;

			return true;
		}

		private static bool IsSafeUrl(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return true;

			value = value.TrimStart();
			var i = 0;

			if (i >= value.Length || !char.IsAsciiLetter(value[i]))
				return true;

			while (i < value.Length && (char.IsAsciiLetterOrDigit(value[i]) || value[i] is '+' or '-' or '.'))
				i++;

			if (i >= value.Length || value[i] != ':')
				return true;

			return _allowedUrlSchemes.Contains(value[..i]);
		}

		private static bool IsSafeStyle(string? value)
		{
			if (string.IsNullOrEmpty(value))
				return true;

			return !value.Contains("url(", StringComparison.OrdinalIgnoreCase)
				&& !value.Contains("expression", StringComparison.OrdinalIgnoreCase)
				&& !value.Contains("@import", StringComparison.OrdinalIgnoreCase)
				&& !value.Contains("-moz-binding", StringComparison.OrdinalIgnoreCase)
				&& !value.Contains("behavior:", StringComparison.OrdinalIgnoreCase)
				&& !value.Contains("javascript:", StringComparison.OrdinalIgnoreCase);
		}
	}
}
