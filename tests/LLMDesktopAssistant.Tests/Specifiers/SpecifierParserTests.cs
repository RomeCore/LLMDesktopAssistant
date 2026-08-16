using LLMDesktopAssistant.Tools.Specifiers;

namespace LLMDesktopAssistant.Tests.Specifiers;

public class SpecifierParserTests
{
	[Fact]
	public void Parse_LiteralOnly_CreatesSingleLiteralPart()
	{
		var specifier = SpecifierParser.Parse("git diff *");

		var part = Assert.IsType<SpecifierLiteralPart>(Assert.Single(specifier.Parts));
		Assert.Equal("git diff *", part.Value);
	}

	[Fact]
	public void Parse_ParameterLiteral_CreatesParameterPart()
	{
		var specifier = SpecifierParser.Parse("command:git diff *", ["command"]);

		var part = Assert.IsType<SpecifierParameterPart>(Assert.Single(specifier.Parts));
		Assert.Equal("command", part.Name);
		Assert.Equal("git diff *", part.Value);
	}

	[Fact]
	public void Parse_UnknownParameterName_FallsBackToLiteral()
	{
		var specifier = SpecifierParser.Parse("command:git diff *");

		var part = Assert.IsType<SpecifierLiteralPart>(Assert.Single(specifier.Parts));
		Assert.IsNotType<SpecifierParameterPart>(part);
		Assert.Equal("command:git diff *", part.Value);
	}

	[Fact]
	public void Parse_AndGroup_CreatesAndPart()
	{
		var specifier = SpecifierParser.Parse("git diff * && git status *");

		var and = Assert.IsType<SpecifierAndPart>(Assert.Single(specifier.Parts));
		Assert.Equal(2, and.Parts.Count);
		Assert.Equal("git diff *", and.Parts[0].Value);
		Assert.Equal("git status *", and.Parts[1].Value);
	}

	[Fact]
	public void Parse_OrGroups_CreatesMultipleParts()
	{
		var specifier = SpecifierParser.Parse("git * || npm *");

		Assert.Equal(2, specifier.Parts.Count);
		Assert.Equal("git *", Assert.IsType<SpecifierLiteralPart>(specifier.Parts[0]).Value);
		Assert.Equal("npm *", Assert.IsType<SpecifierLiteralPart>(specifier.Parts[1]).Value);
	}

	[Fact]
	public void Parse_MixedAndOr_CreatesNestedStructure()
	{
		var specifier = SpecifierParser.Parse("command:git diff * && path:/src/** || docker ps", ["command", "path"]);

		Assert.Equal(2, specifier.Parts.Count);

		var and = Assert.IsType<SpecifierAndPart>(specifier.Parts[0]);
		var commandPart = Assert.IsType<SpecifierParameterPart>(and.Parts[0]);
		Assert.Equal("command", commandPart.Name);
		Assert.Equal("git diff *", commandPart.Value);
		var pathPart = Assert.IsType<SpecifierParameterPart>(and.Parts[1]);
		Assert.Equal("path", pathPart.Name);
		Assert.Equal("/src/**", pathPart.Value);

		Assert.Equal("docker ps", Assert.IsType<SpecifierLiteralPart>(specifier.Parts[1]).Value);
	}

	[Fact]
	public void Parse_EscapedOperators_TreatedAsLiteralText()
	{
		var specifier = SpecifierParser.Parse(@"git diff \|| x && echo \&& y");

		var and = Assert.IsType<SpecifierAndPart>(Assert.Single(specifier.Parts));
		Assert.Equal("git diff || x", and.Parts[0].Value);
		Assert.Equal("echo && y", and.Parts[1].Value);
	}

	[Fact]
	public void Parse_WhitespaceAroundParts_IsSkipped()
	{
		var specifier = SpecifierParser.Parse("  git diff *  ");

		Assert.Equal("git diff *", Assert.IsType<SpecifierLiteralPart>(Assert.Single(specifier.Parts)).Value);
	}

	[Fact]
	public void Parse_MultipleColons_KeepsRestAsValue()
	{
		var specifier = SpecifierParser.Parse("path:C:\\src\\file.cs", ["path"]);

		var part = Assert.IsType<SpecifierParameterPart>(Assert.Single(specifier.Parts));
		Assert.Equal("path", part.Name);
		Assert.Equal("C:\\src\\file.cs", part.Value);
	}

	[Fact]
	public void Parse_EmptyLiteral_Throws()
	{
		Assert.ThrowsAny<Exception>(() => SpecifierParser.Parse("&& git *"));
	}

	[Fact]
	public void Parse_MissingOperandAfterOperator_Throws()
	{
		Assert.ThrowsAny<Exception>(() => SpecifierParser.Parse("git * &&"));
		Assert.ThrowsAny<Exception>(() => SpecifierParser.Parse("git * ||"));
	}

	[Fact]
	public void TryParse_InvalidSyntax_ReturnsNull()
	{
		Assert.Null(SpecifierParser.TryParse(""));
		Assert.Null(SpecifierParser.TryParse("&& git *"));
		Assert.Null(SpecifierParser.TryParse("git * &&"));
		Assert.Null(SpecifierParser.TryParse("git * ||"));
	}

	[Fact]
	public void TryParse_ValidSyntax_ReturnsSpecifier()
	{
		var specifier = SpecifierParser.TryParse("git * || npm *");

		Assert.NotNull(specifier);
		Assert.Equal(2, specifier!.Parts.Count);
	}

	[Fact]
	public void Combined_ConcatenatesParts()
	{
		var first = SpecifierParser.Parse("git *");
		var second = SpecifierParser.Parse("npm *");

		var combined = Specifier.Combined(first, second);

		Assert.Equal(2, combined.Parts.Count);
		Assert.Equal("git *", Assert.IsType<SpecifierLiteralPart>(combined.Parts[0]).Value);
		Assert.Equal("npm *", Assert.IsType<SpecifierLiteralPart>(combined.Parts[1]).Value);
	}
}
