using LLMDesktopAssistant.Markdown;

namespace LLMDesktopAssistant.Tests.Utils;

public class MarkdownEscaperTests
{
	[Fact]
	public void Null_ReturnsNull()
	{
		Assert.Null(MarkdownEscaper.Escape(null));
	}

	[Fact]
	public void Empty_ReturnsEmpty()
	{
		Assert.Equal(string.Empty, MarkdownEscaper.Escape(string.Empty));
	}

	[Fact]
	public void PlainText_IsUnchanged()
	{
		const string input = "Hello, world 123";

		Assert.Equal(input, MarkdownEscaper.Escape(input));
	}

	[Fact]
	public void UnicodeText_IsUnchanged()
	{
		const string input = "Привет, мир 🌍";

		Assert.Equal(input, MarkdownEscaper.Escape(input));
	}

	[Fact]
	public void AllSpecialCharacters_AreEscaped()
	{
		char[] specialCharacters = { '\\', '`', '*', '_', '{', '}', '[', ']', '(', ')', '#', '+', '-', '.', '!', '|', '>', '~' };
		string input = new string(specialCharacters);
		string expected = string.Concat(specialCharacters.Select(c => $"\\{c}"));

		Assert.Equal(expected, MarkdownEscaper.Escape(input));
	}

	[Fact]
	public void Backslash_IsEscaped()
	{
		Assert.Equal("\\\\", MarkdownEscaper.Escape("\\"));
	}

	[Fact]
	public void AlreadyEscapedText_IsDoubleEscaped()
	{
		Assert.Equal("\\\\\\*", MarkdownEscaper.Escape("\\*"));
	}

	[Theory]
	[InlineData("**bold**", "\\*\\*bold\\*\\*")]
	[InlineData("# header", "\\# header")]
	[InlineData("[link](https://example.com)", "\\[link\\]\\(https://example\\.com\\)")]
	[InlineData("`code`", "\\`code\\`")]
	[InlineData("~~strike~~", "\\~\\~strike\\~\\~")]
	public void MarkdownConstructs_AreNeutralized(string input, string expected)
	{
		Assert.Equal(expected, MarkdownEscaper.Escape(input));
	}
}
