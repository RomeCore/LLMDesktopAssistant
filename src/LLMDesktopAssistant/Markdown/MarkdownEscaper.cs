using System.Text;

namespace LLMDesktopAssistant.Markdown
{
	/// <summary>
	/// Provides utility methods for escaping Markdown special characters.
	/// </summary>
	public static class MarkdownEscaper
	{
		/// <summary>
		/// Escapes all Markdown special characters in the specified input so that the
		/// resulting string is rendered as plain text instead of being interpreted as Markdown.
		/// </summary>
		/// <param name="input">The string to escape, or <see langword="null"/>.</param>
		/// <returns>
		/// The escaped string, or <see langword="null"/> if <paramref name="input"/> is <see langword="null"/>.
		/// </returns>
		public static string? Escape(string? input)
		{
			if (input is null)
				return null;

			int firstSpecialCharacter = -1;
			for (int i = 0; i < input.Length; i++)
			{
				if (IsMarkdownSpecialCharacter(input[i]))
				{
					firstSpecialCharacter = i;
					break;
				}
			}

			if (firstSpecialCharacter < 0)
				return input;

			var builder = new StringBuilder(input.Length + 8);
			builder.Append(input, 0, firstSpecialCharacter);

			for (int i = firstSpecialCharacter; i < input.Length; i++)
			{
				char c = input[i];
				if (IsMarkdownSpecialCharacter(c))
					builder.Append('\\');
				builder.Append(c);
			}

			return builder.ToString();
		}

		private static bool IsMarkdownSpecialCharacter(char c)
		{
			switch (c)
			{
				case '\\':
				case '`':
				case '*':
				case '_':
				case '{':
				case '}':
				case '[':
				case ']':
				case '(':
				case ')':
				case '#':
				case '+':
				case '-':
				case '.':
				case '!':
				case '|':
				case '>':
				case '~':
					return true;
				default:
					return false;
			}
		}
	}
}
