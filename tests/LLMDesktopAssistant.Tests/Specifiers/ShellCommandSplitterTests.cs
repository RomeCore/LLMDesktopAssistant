using LLMDesktopAssistant.Tools.Specifiers;

namespace LLMDesktopAssistant.Tests.Specifiers;

public class ShellCommandSplitterTests
{
	// ──────────────────────────── basics ────────────────────────────

	[Fact]
	public void Split_SingleCommand_OnePart()
	{
		var parts = ShellCommandSplitter.Split("git status");

		Assert.Equal(["git status"], parts);
	}

	[Fact]
	public void Split_SemicolonSeparated_TwoParts()
	{
		var parts = ShellCommandSplitter.Split("git status; npm install");

		Assert.Equal(["git status", "npm install"], parts);
	}

	[Fact]
	public void Split_DoubleAmpersand_TwoParts()
	{
		var parts = ShellCommandSplitter.Split("git add . && git commit -m 'x'");

		Assert.Equal(["git add .", "git commit -m 'x'"], parts);
	}

	[Fact]
	public void Split_DoublePipe_TwoParts()
	{
		var parts = ShellCommandSplitter.Split("git pull || git fetch");

		Assert.Equal(["git pull", "git fetch"], parts);
	}

	[Fact]
	public void Split_Newline_TwoParts()
	{
		var parts = ShellCommandSplitter.Split("git status\nnpm install");

		Assert.Equal(["git status", "npm install"], parts);
	}

	[Fact]
	public void Split_CarriageReturnNewline_TwoParts()
	{
		var parts = ShellCommandSplitter.Split("git status\r\nnpm install");

		Assert.Equal(["git status", "npm install"], parts);
	}

	[Fact]
	public void Split_MixedSeparators_FourParts()
	{
		var parts = ShellCommandSplitter.Split("git status; npm install && dotnet build\r\ngit log || echo done");

		Assert.Equal(["git status", "npm install", "dotnet build", "git log", "echo done"], parts);
	}

	// ──────────────────────────── pipes and ampersands ────────────────────────────

	[Fact]
	public void Split_Pipe_NotASeparator()
	{
		var parts = ShellCommandSplitter.Split("git log | grep fix");

		Assert.Equal(["git log | grep fix"], parts);
	}

	[Fact]
	public void Split_SingleAmpersand_NotASeparatorByDefault()
	{
		var parts = ShellCommandSplitter.Split("npm run dev & echo done");

		Assert.Equal(["npm run dev & echo done"], parts);
	}

	[Fact]
	public void Split_SingleAmpersand_SeparatorWhenRequested()
	{
		var parts = ShellCommandSplitter.Split("echo a & echo b", singleAmpersandIsSeparator: true);

		Assert.Equal(["echo a", "echo b"], parts);
	}

	[Fact]
	public void Split_DoubleAmpersand_SeparatorEvenWithoutFlag()
	{
		var parts = ShellCommandSplitter.Split("echo a && echo b");

		Assert.Equal(["echo a", "echo b"], parts);
	}

	[Fact]
	public void Split_SingleAmpersandInMixed_SeparatorWhenRequested()
	{
		var parts = ShellCommandSplitter.Split("echo a & echo b && echo c", singleAmpersandIsSeparator: true);

		Assert.Equal(["echo a", "echo b", "echo c"], parts);
	}

	// ──────────────────────────── quotes ────────────────────────────

	[Fact]
	public void Split_SemicolonInsideDoubleQuotes_NotASeparator()
	{
		var parts = ShellCommandSplitter.Split("git commit -m \"fix; done\" && git push");

		Assert.Equal(["git commit -m \"fix; done\"", "git push"], parts);
	}

	[Fact]
	public void Split_SeparatorsInsideSingleQuotes_NotSeparators()
	{
		var parts = ShellCommandSplitter.Split("echo 'a && b; c'");

		Assert.Equal(["echo 'a && b; c'"], parts);
	}

	[Fact]
	public void Split_EscapedDoubleQuoteInsideQuotes_QuoteStaysOpen()
	{
		var parts = ShellCommandSplitter.Split("echo \"say \\\"hi\\\"; ok\"");

		Assert.Equal(["echo \"say \\\"hi\\\"; ok\""], parts);
	}

	[Fact]
	public void Split_QuotedPipe_NotASeparator()
	{
		var parts = ShellCommandSplitter.Split("echo \"a | b\"");

		Assert.Equal(["echo \"a | b\""], parts);
	}

	// ──────────────────────────── trimming and empties ────────────────────────────

	[Fact]
	public void Split_TrimsWhitespaceAroundParts()
	{
		var parts = ShellCommandSplitter.Split("  git status  ;   npm install ");

		Assert.Equal(["git status", "npm install"], parts);
	}

	[Fact]
	public void Split_EmptyParts_Skipped()
	{
		var parts = ShellCommandSplitter.Split("git status ; ; npm install &&");

		Assert.Equal(["git status", "npm install"], parts);
	}

	[Fact]
	public void Split_EmptyCommand_NoParts()
	{
		var parts = ShellCommandSplitter.Split("   ");

		Assert.Empty(parts);
	}

	[Fact]
	public void Split_Null_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => ShellCommandSplitter.Split(null!));
	}

	// ──────────────────────────── real-world shapes ────────────────────────────

	[Fact]
	public void Split_PowerShellCompound_Parts()
	{
		var parts = ShellCommandSplitter.Split("$x = 1; Write-Host $x; if ($x) { echo ok }");

		Assert.Equal(["$x = 1", "Write-Host $x", "if ($x) { echo ok }"], parts);
	}

	[Fact]
	public void Split_CommandWithEqualsAndColon_Intact()
	{
		var parts = ShellCommandSplitter.Split("git commit -m \"feat: x\" --no-verify");

		Assert.Equal(["git commit -m \"feat: x\" --no-verify"], parts);
	}
}
