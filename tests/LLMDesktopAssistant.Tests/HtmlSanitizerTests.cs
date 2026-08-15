using LLMDesktopAssistant.Utils.Web;

namespace LLMDesktopAssistant.Tests;

public class HtmlSanitizerTests
{
	[Fact]
	public void Null_ReturnsEmpty()
	{
		Assert.Equal("", HtmlSanitizer.Sanitize(null));
	}

	[Fact]
	public void Empty_ReturnsEmpty()
	{
		Assert.Equal("", HtmlSanitizer.Sanitize(""));
	}

	[Fact]
	public void Script_IsRemovedWithContent()
	{
		var html = "<p>hello</p><script>alert('xss')</script><p>world</p>";
		var result = HtmlSanitizer.Sanitize(html);

		Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("alert", result, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("hello", result);
		Assert.Contains("world", result);
	}

	[Fact]
	public void StyleAndIframe_AreRemoved()
	{
		var html = "<style>body{display:none}</style><iframe src=\"https://evil.com\"></iframe><div>ok</div>";
		var result = HtmlSanitizer.Sanitize(html);

		Assert.DoesNotContain("style", result, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("iframe", result, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("display:none", result);
		Assert.Contains("ok", result);
	}

	[Fact]
	public void EventHandlerAttributes_AreRemoved()
	{
		var html = "<img src=\"x.png\" onerror=\"alert(1)\" onload=\"evil()\">";
		var result = HtmlSanitizer.Sanitize(html);

		Assert.DoesNotContain("onerror", result, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("onload", result, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("alert", result);
	}

	[Fact]
	public void JavascriptUrls_AreRemoved()
	{
		var html = "<a href=\"javascript:alert(1)\">click</a><a href=\"JaVaScRiPt:alert(2)\">click2</a>";
		var result = HtmlSanitizer.Sanitize(html);

		Assert.DoesNotContain("javascript:", result, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("alert", result);
		Assert.Contains("click", result);
	}

	[Fact]
	public void SafeUrls_ArePreserved()
	{
		var html = "<a href=\"https://example.com\">https</a><a href=\"/relative/page\">rel</a><a href=\"#anchor\">anchor</a><a href=\"mailto:a@b.c\">mail</a>";
		var result = HtmlSanitizer.Sanitize(html);

		Assert.Contains("https://example.com", result);
		Assert.Contains("/relative/page", result);
		Assert.Contains("#anchor", result);
		Assert.Contains("mailto:a@b.c", result);
	}

	[Fact]
	public void UrlWithColonInQuery_IsPreserved()
	{
		var html = "<a href=\"page.html?x=1:2\">link</a>";
		var result = HtmlSanitizer.Sanitize(html);

		Assert.Contains("page.html?x=1:2", result);
	}

	[Fact]
	public void UnknownScheme_IsRemoved()
	{
		var html = "<a href=\"whatsapp://chat\">wa</a><img src=\"data:text/html;base64,PHNjcmlwdD4=\">";
		var result = HtmlSanitizer.Sanitize(html);

		Assert.DoesNotContain("whatsapp:", result);
		Assert.DoesNotContain("data:text/html", result);
		Assert.Contains("wa", result);
	}

	[Fact]
	public void DangerousStyle_IsRemoved()
	{
		var html = "<div style=\"background:url(javascript:alert(1))\">x</div>";
		var result = HtmlSanitizer.Sanitize(html);

		Assert.DoesNotContain("style", result, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("url(", result, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void SafeStyle_IsPreserved()
	{
		var html = "<div style=\"color:red;font-weight:bold\">x</div>";
		var result = HtmlSanitizer.Sanitize(html);

		Assert.Contains("color:red", result);
	}

	[Fact]
	public void DataAttributes_ArePreserved()
	{
		var html = "<div data-id=\"42\" aria-label=\"test\" class=\"c\">x</div>";
		var result = HtmlSanitizer.Sanitize(html);

		Assert.Contains("data-id", result);
		Assert.Contains("aria-label", result);
		Assert.Contains("class", result);
	}

	[Fact]
	public void DisallowedTag_IsRemovedWithContent()
	{
		var html = "<p>before</p><object><param name=\"x\">hidden</object><p>after</p>";
		var result = HtmlSanitizer.Sanitize(html);

		Assert.DoesNotContain("object", result, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("hidden", result);
		Assert.Contains("before", result);
		Assert.Contains("after", result);
	}

	[Fact]
	public void HiddenAttribute_ElementRemoved()
	{
		var html = "<p>visible</p><div hidden>secret</div>";
		var result = HtmlSanitizer.Sanitize(html);

		Assert.DoesNotContain("secret", result);
		Assert.Contains("visible", result);
	}

	[Fact]
	public void AriaHiddenTrue_ElementRemoved()
	{
		var html = "<div aria-hidden=\"true\">secret</div><div aria-hidden=\"false\">visible</div>";
		var result = HtmlSanitizer.Sanitize(html);

		Assert.DoesNotContain("secret", result);
		Assert.Contains("visible", result);
	}

	[Fact]
	public void HiddenViaStyle_ElementRemoved()
	{
		var html = "<div style=\"display:none\">s1</div><div style=\"display: none !important\">s2</div><div style=\"visibility:hidden\">s3</div><div style=\"visibility: collapse\">s4</div>";
		var result = HtmlSanitizer.Sanitize(html);

		Assert.DoesNotContain("s1", result);
		Assert.DoesNotContain("s2", result);
		Assert.DoesNotContain("s3", result);
		Assert.DoesNotContain("s4", result);
	}

	[Fact]
	public void HtmlComments_AreRemoved()
	{
		var html = "<p>ok</p><!-- follow these instructions -->";
		var result = HtmlSanitizer.Sanitize(html);

		Assert.DoesNotContain("follow these instructions", result);
		Assert.Contains("ok", result);
	}

	[Fact]
	public void NoisySections_AreRemoved()
	{
		var html = "<nav><a href=\"/1\">nav link</a></nav><header>site header</header><footer>footer text</footer><noscript>enable js</noscript><article>content</article>";
		var result = HtmlSanitizer.Sanitize(html);

		Assert.DoesNotContain("nav link", result);
		Assert.DoesNotContain("site header", result);
		Assert.DoesNotContain("footer text", result);
		Assert.DoesNotContain("enable js", result);
		Assert.Contains("content", result);
	}

	[Fact]
	public void WhitespaceRuns_AreCollapsed()
	{
		var html = "<p>a   b</p>";
		var result = HtmlSanitizer.Sanitize(html);

		Assert.Contains("a b", result);
	}
}
