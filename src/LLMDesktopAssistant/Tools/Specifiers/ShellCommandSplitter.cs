using System.Text;

namespace LLMDesktopAssistant.Tools.Specifiers
{
	/// <summary>
	/// Splits compound shell commands into individual commands for specifier matching.
	/// </summary>
	public static class ShellCommandSplitter
	{
		/// <summary>
		/// Splits a compound shell command into individual commands.
		/// <list type="bullet">
		/// <item><description><c>;</c>, <c>&amp;&amp;</c>, <c>||</c> and newlines are always treated as command separators.</description></item>
		/// <item><description>A single <c>&amp;</c> is treated as a separator only when <paramref name="singleAmpersandIsSeparator"/>
		/// is <see langword="true"/> (cmd.exe style); otherwise it is kept inside the command
		/// (e.g. the Bash background operator or the PowerShell call operator).</description></item>
		/// <item><description>A pipe (<c>|</c>) is never treated as a separator.</description></item>
		/// <item><description>Separators inside single- or double-quoted strings are not treated as command boundaries;
		/// a backslash escapes a double quote inside a double-quoted string.</description></item>
		/// </list>
		/// The resulting commands are trimmed; empty commands are skipped.
		/// </summary>
		/// <param name="command">The compound shell command. Cannot be <see langword="null"/>.</param>
		/// <param name="singleAmpersandIsSeparator">Whether a single <c>&amp;</c> separates commands.</param>
		/// <returns>The individual commands of the compound command.</returns>
		public static IEnumerable<string> Split(string command, bool singleAmpersandIsSeparator = false)
		{
			ArgumentNullException.ThrowIfNull(command);

			var parts = new List<string>();
			var current = new StringBuilder();
			char? quote = null;

			void Flush()
			{
				var part = current.ToString().Trim();
				current.Clear();
				if (part.Length > 0)
					parts.Add(part);
			}

			for (int i = 0; i < command.Length; i++)
			{
				char c = command[i];

				if (quote != null)
				{
					current.Append(c);
					if (c == quote && !(c == '"' && IsEscapedDoubleQuote(command, i)))
						quote = null;
					continue;
				}

				if (c is '"' or '\'')
				{
					quote = c;
					current.Append(c);
					continue;
				}

				if (c is ';' or '\n' or '\r')
				{
					Flush();
					continue;
				}

				if (c == '&' && (singleAmpersandIsSeparator || IsNext(command, i, '&')))
				{
					Flush();
					if (IsNext(command, i, '&'))
						i++;
					continue;
				}

				if (c == '|' && IsNext(command, i, '|'))
				{
					Flush();
					i++;
					continue;
				}

				current.Append(c);
			}

			Flush();

			return parts;
		}

		/// <summary>
		/// Checks whether the character at the specified index is followed by the expected character.
		/// </summary>
		private static bool IsNext(string command, int index, char expected)
		{
			return index + 1 < command.Length && command[index + 1] == expected;
		}

		/// <summary>
		/// Checks whether the double quote at the specified index is escaped by an odd number of preceding backslashes.
		/// </summary>
		private static bool IsEscapedDoubleQuote(string command, int quoteIndex)
		{
			int backslashes = 0;
			for (int i = quoteIndex - 1; i >= 0 && command[i] == '\\'; i--)
				backslashes++;
			return backslashes % 2 == 1;
		}
	}
}
