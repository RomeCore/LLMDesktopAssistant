using System.Text;

namespace LLMDesktopAssistant.Agents.Memory
{
	/// <summary>
	/// Provides tokenization utilities for the BM25 keyword search used by the memory store.
	/// Tokens are lowercased and split on any character that is not a letter or a digit,
	/// which keeps both Latin and Cyrillic text searchable.
	/// </summary>
	internal static class MemoryTokenizer
	{
		/// <summary>
		/// Splits the specified text into lowercase tokens.
		/// </summary>
		/// <param name="text">The text to tokenize.</param>
		/// <returns>The list of tokens, or an empty list when the text is empty or whitespace.</returns>
		public static IReadOnlyList<string> Tokenize(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return [];

			var tokens = new List<string>();
			var token = new StringBuilder();

			foreach (char c in text)
			{
				if (char.IsLetterOrDigit(c))
				{
					token.Append(char.ToLowerInvariant(c));
				}
				else if (token.Length > 0)
				{
					tokens.Add(token.ToString());
					token.Clear();
				}
			}

			if (token.Length > 0)
				tokens.Add(token.ToString());

			return tokens;
		}
	}
}
