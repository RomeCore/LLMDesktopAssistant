using System.Text.RegularExpressions;
using LLTSharp;
using RCParsing;
using RCParsing.TokenPatterns;
using RCParsing.TokenPatterns.Combinators;

namespace LLMDesktopAssistant.Prompting.LLT;

/// <summary>
/// Classifies LLT source text into colorizable segments using an
/// <see cref="LLTDiagnosticsParser"/> with error recovery.
/// </summary>
public sealed partial class LLTTokenClassifier
{
	[GeneratedRegex(@"^-?\d+(\.\d+)?$", RegexOptions.Compiled)]
	private static partial Regex GetNumberRegex();
	private static readonly Regex NumberRegex = GetNumberRegex();

	private readonly LLTDiagnosticsParser _parser = new();
	private readonly LLTParser.LLTParsingContext _context = new()
	{
		LocalLibrary = new TemplateLibrary()
	};

	/// <summary>
	/// Parses the specified text and returns classified segments followed by error segments.
	/// </summary>
	/// <param name="text">The LLT source text to classify.</param>
	/// <returns>
	/// A tuple containing the classified segments (error segments are appended last)
	/// and the list of parse errors.
	/// </returns>
	public (IReadOnlyList<LLTTokenSegment> Segments, IReadOnlyList<LLTParseError> Errors) Classify(string text)
	{
		text ??= string.Empty;

		var success = _parser.Parser.TryParse(text, _context, out var root);

		var segments = new List<LLTTokenSegment>();
		if (success)
		{
			foreach (var token in root.GetJoinedChildren())
			{
				// Some nodes (empty optionals, skipped fragments after recovery) report
				// invalid spans; they cannot be colorized.
				if (token.StartIndex < 0 || token.Length <= 0 || token.StartIndex + token.Length > text.Length)
					continue;

				var kind = ClassifyToken(token);
				if (kind == LLTTokenKind.PlainText)
					continue;

				segments.Add(new LLTTokenSegment(token.StartIndex, token.Length, kind));
			}
		}

		var errors = new List<LLTParseError>();
		var errorGroups = root.CreateErrorGroups();
		foreach (var group in errorGroups.RelevantGroups)
		{
			var position = Math.Min(group.Position, text.Length);
			errors.Add(new LLTParseError(position, GetLine(text, position), GetColumn(text, position),
				group.Errors.FirstOrDefault().message ?? string.Empty));

			// Colorize the rest of the erroneous line as an error fragment.
			var lineEnd = text.IndexOf('\n', position);
			var length = (lineEnd < 0 ? text.Length : lineEnd) - position;
			if (length > 0)
				segments.Add(new LLTTokenSegment(position, length, LLTTokenKind.Error));
		}

		return (segments, errors);
	}

	private static LLTTokenKind ClassifyToken(ParsedRuleResultBase token)
	{
		var text = token.Span.ToString();
		var pattern = token.Token?.Token;

		switch (pattern)
		{
			case KeywordTokenPattern:
			case KeywordChoiceTokenPattern:
				return LLTTokenKind.Keyword;

			case IdentifierTokenPattern:
			case CaptureTextTokenPattern:
				return LLTTokenKind.Identifier;
				
			case NumberTokenPattern:
				return LLTTokenKind.Number;

			case MapTokenPattern:
				return NumberRegex.IsMatch(text) ? LLTTokenKind.Number : LLTTokenKind.Identifier;

			case EscapedTextTokenPattern:
				return LLTTokenKind.PlainText;

			case ChoiceTokenPattern:
			case BetweenTokenPattern:
			case SequenceTokenPattern:
				return text.StartsWith('\'') || text.StartsWith('"')
					? LLTTokenKind.String
					: LLTTokenKind.PlainText;

			case LiteralCharTokenPattern:
			case LiteralTokenPattern:
			case LiteralChoiceTokenPattern:
			case RegexTokenPattern:
				return ClassifyLiteral(text);
		}

		return LLTTokenKind.PlainText;
	}

	private static LLTTokenKind ClassifyLiteral(string text)
	{
		if (text.Length == 0)
			return LLTTokenKind.PlainText;

		if (IsWord(text))
			return LLTTokenKind.Keyword;

		return IsControlChar(text[0]) ? LLTTokenKind.Control : LLTTokenKind.Operator;
	}

	private static bool IsWord(string text)
	{
		return text.All(c => char.IsLetterOrDigit(c) || c == '_');
	}

	private static bool IsControlChar(char c)
	{
		return c is '@' or '{' or '}' or '(' or ')' or '[' or ']' or ',' or '.' or ':' or ';';
	}

	private static int GetLine(string text, int position)
	{
		var line = 1;
		for (var i = 0; i < position && i < text.Length; i++)
		{
			if (text[i] == '\n')
				line++;
		}
		return line;
	}

	private static int GetColumn(string text, int position)
	{
		var lineStart = text.LastIndexOf('\n', Math.Max(0, position - 1));
		return position - lineStart;
	}
}
