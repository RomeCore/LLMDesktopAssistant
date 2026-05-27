using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Syntax;
using LLMDesktopAssistant.Markdown.Nodes;

namespace LLMDesktopAssistant.Markdown.Parsers;

/// <summary>
/// Inline parser for quick action buttons: [> Button text](prompt)
/// </summary>
public class QuickActionParser : InlineParser
{
	public QuickActionParser()
	{
		OpeningCharacters = ['['];
	}

	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		// Save the start position
		var startingPosition = slice.Start;

		// Check that we have [> 
		var c = slice.NextChar();
		if (c != '>')
			return false;

		// Skip optional space after >
		c = slice.NextChar();
		if (c.IsSpaceOrTab())
			c = slice.NextChar();

		// Now parse the button text until ]
		var buttonTextStart = slice.Start;
		int bracketDepth = 1; // we have one open [
		var text = new System.Text.StringBuilder();

		while (c != '\0')
		{
			if (c == '[')
			{
				bracketDepth++;
				text.Append(c);
			}
			else if (c == ']')
			{
				bracketDepth--;
				if (bracketDepth == 0)
				{
					// Found closing ]
					slice.NextChar(); // skip ]
					break;
				}
				text.Append(c);
			}
			else if (c == '\\')
			{
				// Escaped character
				c = slice.NextChar();
				if (c != '\0')
				{
					text.Append(c);
				}
			}
			else
			{
				text.Append(c);
			}

			c = slice.NextChar();
		}

		if (bracketDepth != 0)
			return false; // Unclosed [

		var buttonText = text.ToString().Trim();
		if (string.IsNullOrEmpty(buttonText))
			return false;

		// Now expect (prompt)
		c = slice.CurrentChar;
		if (c != '(')
			return false;

		slice.NextChar(); // skip (
		c = slice.CurrentChar;

		var promptStart = slice.Start;
		int parenDepth = 1;
		var promptText = new System.Text.StringBuilder();

		while (c != '\0')
		{
			if (c == '(')
			{
				parenDepth++;
				promptText.Append(c);
			}
			else if (c == ')')
			{
				parenDepth--;
				if (parenDepth == 0)
				{
					// Found closing )
					break;
				}
				promptText.Append(c);
			}
			else if (c == '\\')
			{
				c = slice.NextChar();
				if (c != '\0')
					promptText.Append(c);
			}
			else
			{
				promptText.Append(c);
			}

			c = slice.NextChar();
		}

		if (parenDepth != 0)
			return false; // Unclosed (

		var prompt = promptText.ToString().Trim();
		slice.NextChar(); // skip )

		// Create the QuickAction node
		var quickAction = new QuickAction
		{
			ButtonText = buttonText,
			Prompt = prompt,
			Span = new SourceSpan(
				processor.GetSourcePosition(startingPosition, out int line, out int column),
				processor.GetSourcePosition(slice.Start - 1)),
			Line = line,
			Column = column
		};

		processor.Inline = quickAction;
		return true;
	}
}
