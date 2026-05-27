using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Syntax;
using LLMDesktopAssistant.Markdown.Nodes;

namespace LLMDesktopAssistant.Markdown.Parsers;

/// <summary>
/// Inline parser for quick explanation tooltips: @[Term](definition)
/// </summary>
public class QuickExplanationParser : InlineParser
{
	public QuickExplanationParser()
	{
		OpeningCharacters = ['@'];
	}

	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		// Don't match if preceded by a letter or digit (to avoid matching emails)
		var prevChar = slice.PeekCharExtra(-1);
		if (prevChar.IsAlphaNumeric())
			return false;

		var startingPosition = slice.Start;

		// Check that we have @[
		var c = slice.NextChar();
		if (c != '[')
			return false;

		c = slice.NextChar();

		// Parse the term until ]
		var termText = new System.Text.StringBuilder();
		int bracketDepth = 1;

		while (c != '\0')
		{
			if (c == '[')
			{
				bracketDepth++;
				termText.Append(c);
			}
			else if (c == ']')
			{
				bracketDepth--;
				if (bracketDepth == 0)
				{
					slice.NextChar(); // skip ]
					break;
				}
				termText.Append(c);
			}
			else if (c == '\\')
			{
				c = slice.NextChar();
				if (c != '\0')
					termText.Append(c);
			}
			else
			{
				termText.Append(c);
			}

			c = slice.NextChar();
		}

		if (bracketDepth != 0)
			return false;

		var term = termText.ToString().Trim();
		if (string.IsNullOrEmpty(term))
			return false;

		// Now expect (definition)
		c = slice.CurrentChar;
		if (c != '(')
			return false;

		slice.NextChar(); // skip (
		c = slice.CurrentChar;

		var defText = new System.Text.StringBuilder();
		int parenDepth = 1;

		while (c != '\0')
		{
			if (c == '(')
			{
				parenDepth++;
				defText.Append(c);
			}
			else if (c == ')')
			{
				parenDepth--;
				if (parenDepth == 0)
				{
					break;
				}
				defText.Append(c);
			}
			else if (c == '\\')
			{
				c = slice.NextChar();
				if (c != '\0')
					defText.Append(c);
			}
			else
			{
				defText.Append(c);
			}

			c = slice.NextChar();
		}

		if (parenDepth != 0)
			return false;

		var definition = defText.ToString().Trim();
		slice.NextChar(); // skip )

		var explanation = new QuickExplanation
		{
			Term = term,
			Definition = definition,
			Span = new SourceSpan(
				processor.GetSourcePosition(startingPosition, out int line, out int column),
				processor.GetSourcePosition(slice.Start - 1)),
			Line = line,
			Column = column
		};

		processor.Inline = explanation;
		return true;
	}
}
