using LLMDesktopAssistant.Tools.Specifiers;

namespace LLMDesktopAssistant.Tests.Specifiers;

public class SpecifierMatcherTests
{
	// ──────────────────────────── Basic literals ────────────────────────────

	[Fact]
	public void Match_SingleLiteralCoversSinglePart_FullMatch()
	{
		var specifier = SpecifierParser.Parse("git diff *");

		var result = SpecifierMatcher.Match(specifier, ["git diff --stat"], []);

		Assert.Equal(SpecifierMatchResult.FullMatch, result);
	}

	[Fact]
	public void Match_LiteralCoversOnlySomeParts_PartialMatch()
	{
		var specifier = SpecifierParser.Parse("git diff *");

		var result = SpecifierMatcher.Match(specifier, ["git diff --stat", "git status"], []);

		Assert.Equal(SpecifierMatchResult.PartialMatch, result);
	}

	[Fact]
	public void Match_LiteralMatchesNothing_NoMatch()
	{
		var specifier = SpecifierParser.Parse("git diff *");

		var result = SpecifierMatcher.Match(specifier, ["npm install"], []);

		Assert.Equal(SpecifierMatchResult.NoMatch, result);
	}

	[Fact]
	public void Match_UnnamedLiteralWithEmptyParts_NoMatch()
	{
		var specifier = SpecifierParser.Parse("git *");

		var result = SpecifierMatcher.Match(specifier, [], []);

		Assert.Equal(SpecifierMatchResult.NoMatch, result);
	}

	[Fact]
	public void Match_StarWildcard_MatchesEverything()
	{
		var specifier = SpecifierParser.Parse("*");

		var result = SpecifierMatcher.Match(specifier, ["anything at all", "git status"], []);

		Assert.Equal(SpecifierMatchResult.FullMatch, result);
	}

	[Fact]
	public void Match_QuestionMarkWildcard_MatchesSingleCharacter()
	{
		var specifier = SpecifierParser.Parse("git diff -?");

		Assert.Equal(SpecifierMatchResult.FullMatch, SpecifierMatcher.Match(specifier, ["git diff -s"], []));
		Assert.Equal(SpecifierMatchResult.NoMatch, SpecifierMatcher.Match(specifier, ["git diff --stat"], []));
	}

	[Fact]
	public void Match_IsCaseSensitive()
	{
		var specifier = SpecifierParser.Parse("Git *");

		var result = SpecifierMatcher.Match(specifier, ["git status"], []);

		Assert.Equal(SpecifierMatchResult.NoMatch, result);
	}

	// ──────────────────────────── OR groups ────────────────────────────

	[Fact]
	public void Match_OrGroupsUnionCoverage_FullMatch()
	{
		var specifier = SpecifierParser.Parse("git * || npm *");

		var result = SpecifierMatcher.Match(specifier, ["git pull", "npm install"], []);

		Assert.Equal(SpecifierMatchResult.FullMatch, result);
	}

	[Fact]
	public void Match_OrGroupsCoverOnlySomeParts_PartialMatch()
	{
		var specifier = SpecifierParser.Parse("git * || npm *");

		var result = SpecifierMatcher.Match(specifier, ["git pull", "docker ps"], []);

		Assert.Equal(SpecifierMatchResult.PartialMatch, result);
	}

	[Fact]
	public void Match_OrGroupsMatchNothing_NoMatch()
	{
		var specifier = SpecifierParser.Parse("git * || npm *");

		var result = SpecifierMatcher.Match(specifier, ["docker ps"], []);

		Assert.Equal(SpecifierMatchResult.NoMatch, result);
	}

	// ──────────────────────────── AND groups ────────────────────────────

	[Fact]
	public void Match_OrGroupsEachCoverDistinctPart_FullMatch()
	{
		var specifier = SpecifierParser.Parse("git diff * || git status:*");

		var result = SpecifierMatcher.Match(specifier, ["git diff --stat", "git status"], []);

		Assert.Equal(SpecifierMatchResult.FullMatch, result);
	}

	[Fact]
	public void Match_AndGroupWithConflictingLiterals_NoMatch()
	{
		// Both literals describe the same object: a part cannot be both "git diff *" and "git status:*".
		var specifier = SpecifierParser.Parse("git diff * && git status:*");

		var result = SpecifierMatcher.Match(specifier, ["git diff --stat", "git status"], []);

		Assert.Equal(SpecifierMatchResult.NoMatch, result);
	}

	[Fact]
	public void Match_AndGroupUnnamedLiteralMustCoverAllParts_NoMatch()
	{
		var specifier = SpecifierParser.Parse("git diff * && runTerminal:true", ["runTerminal"]);

		var result = SpecifierMatcher.Match(specifier, ["git diff --stat", "git status"],
			[new KeyValuePair<string, string>("runTerminal", "true")]);

		Assert.Equal(SpecifierMatchResult.NoMatch, result);
	}

	[Fact]
	public void Match_AndGroupWithParameter_FullMatch()
	{
		var specifier = SpecifierParser.Parse("git diff * && runTerminal:true", ["runTerminal"]);

		var result = SpecifierMatcher.Match(specifier, ["git diff --stat"],
			[new KeyValuePair<string, string>("runTerminal", "true")]);

		Assert.Equal(SpecifierMatchResult.FullMatch, result);
	}

	[Fact]
	public void Match_AndGroupParameterDoesNotMatch_NoMatch()
	{
		var specifier = SpecifierParser.Parse("git diff * && runTerminal:true", ["runTerminal"]);

		var result = SpecifierMatcher.Match(specifier, ["git diff --stat"],
			[new KeyValuePair<string, string>("runTerminal", "false")]);

		Assert.Equal(SpecifierMatchResult.NoMatch, result);
	}

	[Fact]
	public void Match_AndGroupParameterMissing_NoMatch()
	{
		var specifier = SpecifierParser.Parse("git diff * && runTerminal:true", ["runTerminal"]);

		var result = SpecifierMatcher.Match(specifier, ["git diff --stat"], []);

		Assert.Equal(SpecifierMatchResult.NoMatch, result);
	}

	// ──────────────────────────── Parameter parts ────────────────────────────

	[Fact]
	public void Match_ParameterOnly_FullMatch()
	{
		var specifier = SpecifierParser.Parse("command:git diff *", ["command"]);

		var result = SpecifierMatcher.Match(specifier, [], [new KeyValuePair<string, string>("command", "git diff --stat")]);

		Assert.Equal(SpecifierMatchResult.FullMatch, result);
	}

	[Fact]
	public void Match_ParameterNotFound_NoMatch()
	{
		var specifier = SpecifierParser.Parse("command:git diff *", ["command"]);

		var result = SpecifierMatcher.Match(specifier, [], []);

		Assert.Equal(SpecifierMatchResult.NoMatch, result);
	}

	[Fact]
	public void Match_ParameterValueDoesNotMatch_NoMatch()
	{
		var specifier = SpecifierParser.Parse("command:git diff *", ["command"]);

		var result = SpecifierMatcher.Match(specifier, [], [new KeyValuePair<string, string>("command", "git status")]);

		Assert.Equal(SpecifierMatchResult.NoMatch, result);
	}

	[Fact]
	public void Match_ParameterAndLiteral_FullMatch()
	{
		var specifier = SpecifierParser.Parse("command:git diff * && git *", ["command"]);

		var result = SpecifierMatcher.Match(specifier, ["git pull"],
			[new KeyValuePair<string, string>("command", "git diff --stat")]);

		Assert.Equal(SpecifierMatchResult.FullMatch, result);
	}

	[Fact]
	public void Match_ParameterMatchedButLiteralNot_NoMatch()
	{
		var specifier = SpecifierParser.Parse("command:git diff * && git *", ["command"]);

		var result = SpecifierMatcher.Match(specifier, ["npm install"],
			[new KeyValuePair<string, string>("command", "git diff --stat")]);

		Assert.Equal(SpecifierMatchResult.NoMatch, result);
	}

	[Fact]
	public void Match_UnmatchedGroupDoesNotAffectTargets_FullMatch()
	{
		var specifier = SpecifierParser.Parse("command:git * || docker *", ["command"]);

		var result = SpecifierMatcher.Match(specifier, ["docker ps"],
			[new KeyValuePair<string, string>("command", "npm install")]);

		Assert.Equal(SpecifierMatchResult.FullMatch, result);
	}

	[Fact]
	public void Match_FirstDuplicateParameterValueIsUsed()
	{
		var specifier = SpecifierParser.Parse("command:git *", ["command"]);

		var result = SpecifierMatcher.Match(specifier, [],
		[
			new KeyValuePair<string, string>("command", "git status"),
			new KeyValuePair<string, string>("command", "npm install")
		]);

		Assert.Equal(SpecifierMatchResult.FullMatch, result);
	}

	// ──────────────────────────── Edge cases ────────────────────────────

	[Fact]
	public void Match_EmptySpecifier_NoMatch()
	{
		var specifier = new Specifier { Parts = [] };

		var result = SpecifierMatcher.Match(specifier, ["git status"], []);

		Assert.Equal(SpecifierMatchResult.NoMatch, result);
	}

	[Fact]
	public void Match_NullSpecifier_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => SpecifierMatcher.Match(null!, ["git status"], []));
	}

	[Fact]
	public void Match_MidPatternWildcard()
	{
		var specifier = SpecifierParser.Parse("git * status");

		Assert.Equal(SpecifierMatchResult.FullMatch, SpecifierMatcher.Match(specifier, ["git pull status"], []));
		Assert.Equal(SpecifierMatchResult.NoMatch, SpecifierMatcher.Match(specifier, ["git pull push"], []));
	}

	// ──────────────────────────── Colon-star suffix ────────────────────────────

	[Fact]
	public void Match_ColonStar_MatchesCommandWithoutArguments()
	{
		var specifier = SpecifierParser.Parse("git status:*");

		var result = SpecifierMatcher.Match(specifier, ["git status"], []);

		Assert.Equal(SpecifierMatchResult.FullMatch, result);
	}

	[Fact]
	public void Match_ColonStar_MatchesCommandWithArguments()
	{
		var specifier = SpecifierParser.Parse("git status:*");

		var result = SpecifierMatcher.Match(specifier, ["git status --short --branch"], []);

		Assert.Equal(SpecifierMatchResult.FullMatch, result);
	}

	[Fact]
	public void Match_ColonStar_DoesNotMatchDifferentCommand()
	{
		var specifier = SpecifierParser.Parse("git status:*");

		var result = SpecifierMatcher.Match(specifier, ["git push"], []);

		Assert.Equal(SpecifierMatchResult.NoMatch, result);
	}

	[Fact]
	public void Match_ColonStar_DoesNotMatchConcatenatedWord()
	{
		var specifier = SpecifierParser.Parse("git status:*");

		var result = SpecifierMatcher.Match(specifier, ["git statusx"], []);

		Assert.Equal(SpecifierMatchResult.NoMatch, result);
	}

	[Fact]
	public void Match_PlainStar_StillRequiresTrailingSpace()
	{
		var specifier = SpecifierParser.Parse("git status *");

		Assert.Equal(SpecifierMatchResult.NoMatch, SpecifierMatcher.Match(specifier, ["git status"], []));
		Assert.Equal(SpecifierMatchResult.FullMatch, SpecifierMatcher.Match(specifier, ["git status --short"], []));
	}
}
