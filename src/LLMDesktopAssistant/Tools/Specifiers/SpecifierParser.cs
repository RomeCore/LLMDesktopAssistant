using System.Collections.Frozen;
using RCParsing;
using RCParsing.TokenPatterns;

namespace LLMDesktopAssistant.Tools.Specifiers
{
	public static class SpecifierParser
	{
		private static readonly Parser _parser;

		static SpecifierParser()
		{
			var builder = new ParserBuilder();

			builder.Settings.SkipWhitespacesOptimized();

			builder.CreateRule("literal")
				.EscapedText([KeyValuePair.Create("\\||", "||"), KeyValuePair.Create("\\&&", "&&")], ["||", "&&"], allowsEmpty: false)

				.Transform(v =>
				{
					return new SpecifierLiteralPart
					{
						Value = v.GetIntermediateValue<string>().Trim()
					};
				});

			static ParsedElement ParamNameMatch(CustomTokenPattern self, string input,
				int start, int end, object? parameter, bool calculateIntermediateValue,
				ref ParsingError furthestError, TokenPattern[] children)
			{
				var paramName = children[0].Match(input, start, end, true);
				if (!paramName.success)
					return ParsedElement.Fail;

				var allowedParamNames = (FrozenSet<string>)parameter!;
				if (allowedParamNames.Contains((string)paramName.intermediateValue!))
					return paramName;

				return ParsedElement.Fail;
			}

			builder.CreateToken("parameter_name")
				.Custom(ParamNameMatch, b => b.CaptureText(b => b.Identifier()));
				
			builder.CreateRule("parameter")
				.Optional(b => b
					.Token("parameter_name")
					.Literal(':'))
				.Rule("literal")

				.Transform(v =>
				{
					var value = v[1].GetValue<SpecifierLiteralPart>();
					if (v[0].Length > 0)
					{
						return new SpecifierParameterPart
						{
							Name = v[0][0][0].GetIntermediateValue<string>(),
							Value = value.Value
						};
					}
					return value;
				});

			builder.CreateRule("and")
				.OneOrMoreSeparated(
					b => b.Rule("parameter"),
					b => b.Literal("&&"),
					includeSeparatorsInResult: false)
				
				.Transform(v =>
				{
					if (v.Children.Count == 1)
						return v[0].GetValue();

					return new SpecifierAndPart
					{
						Parts = v.SelectValues<SpecifierLiteralPart>().ToImmutableList()
					};
				});

			builder.CreateMainRule("specifier")
				.OneOrMoreSeparated(
					b => b.Rule("and"),
					b => b.Literal("||"),
					includeSeparatorsInResult: false)
				.EOF()

				.Transform(v =>
				{
					return new Specifier
					{
						Parts = v[0].SelectValues<SpecifierPart>().ToImmutableList()
					};
				});

			_parser = builder.Build();
		}

		public static Specifier Parse(string input, IEnumerable<string>? allowedParamNames = null)
		{
			var allowedParamNamesSet = allowedParamNames?.ToFrozenSet() ?? [];
			return _parser.Parse<Specifier>(input, parameter: allowedParamNamesSet);
		}

		public static Specifier? TryParse(string input, IEnumerable<string>? allowedParamNames = null)
		{
			var allowedParamNamesSet = allowedParamNames?.ToFrozenSet() ?? [];
			if (_parser.TryParse<Specifier>(input, allowedParamNamesSet, out var result))
				return result;
			return null;
		}
	}
}
