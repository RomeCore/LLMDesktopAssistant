using RCParsing;

namespace LLMDesktopAssistant.Tools
{
	public static class ToolNameWithSpecifierParser
	{
		private static readonly Parser _parser;

		/// <summary>
		/// Gets the parser for parsing tool names with specifiers.
		/// </summary>
		public static Parser Parser => _parser;

		static ToolNameWithSpecifierParser()
		{
			var builder = new ParserBuilder();

			builder.CreateMainRule()
				.UnicodeIdentifier().Label("tool_name")
				.Optional(b => b
					.Literal('(')
					.TextUntil(")").Label("specifier")
					.Literal(')')
					.Transform(v => v["specifier"].Text)
				).Label("specifier")
				.Transform(v => new ToolNameWithSpecifier(v["tool_name"].Text, v["specifier"].TryGetValue<string>()));

			_parser = builder.Build();
		}

		public static ToolNameWithSpecifier Parse(string toolName)
		{
			return _parser.Parse<ToolNameWithSpecifier>(toolName);
		}

		public static IEnumerable<ToolNameWithSpecifier> FindAllMatches(string input)
		{
			return _parser.FindAllMatches<ToolNameWithSpecifier>(input);
		}
	}
}
