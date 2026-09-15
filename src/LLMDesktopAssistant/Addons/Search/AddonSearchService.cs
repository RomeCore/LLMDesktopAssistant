using System.Text;

namespace LLMDesktopAssistant.Addons.Search
{
	/// <summary>
	/// The default <see cref="IAddonSearchService{T}"/> implementation based on the BM25 ranking function.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The name, the tags and the description of every candidate are indexed as three independent fields
	/// with the <see cref="NameWeight"/>, <see cref="TagsWeight"/> and <see cref="DescriptionWeight"/>
	/// weights: a match in the name is worth more than a match in a tag, and a match in a tag is worth
	/// more than a match in the description. The field scores are summed with the classic BM25 term
	/// saturation (<see cref="Bm25K1"/>) and length normalization (<see cref="Bm25B"/>).
	/// </para>
	/// <para>
	/// A query term matches a document token by prefix, which keeps the service suitable for a live
	/// search ('refac' finds 'refactoring' while the user is still typing). An exact name match gets an
	/// additional <see cref="ExactNameBonus"/> so that an addon named exactly as the query is ranked
	/// above the addons that merely start with it.
	/// </para>
	/// </remarks>
	/// <typeparam name="T">The type of the addon to search.</typeparam>
	public class AddonSearchService<T> : IAddonSearchService<T> where T : AddonBase<T>
	{
		private const double Bm25K1 = 1.2;
		private const double Bm25B = 0.75;
		private const double NameWeight = 3.0;
		private const double TagsWeight = 2.0;
		private const double DescriptionWeight = 1.0;
		private const double ExactNameBonus = 4.0;

		/// <inheritdoc/>
		public IReadOnlyList<AddonSearchResult<T>> Search(string query, IEnumerable<T> candidates, int maxResults = 10)
		{
			if (string.IsNullOrWhiteSpace(query))
				return [];

			var terms = Tokenize(query);
			if (terms.Count == 0)
				return [];

			var documents = new List<Document>();
			foreach (var addon in candidates)
				documents.Add(new Document(addon, query.Trim()));

			if (documents.Count == 0)
				return [];

			var scores = new double[documents.Count];
			for (int i = 0; i < documents.Count; i++)
				scores[i] = documents[i].Bonus;

			ScoreField(NameWeight, [.. documents.Select(document => document.NameTokens)], terms, scores);
			ScoreField(TagsWeight, [.. documents.Select(document => document.TagTokens)], terms, scores);
			ScoreField(DescriptionWeight, [.. documents.Select(document => document.DescriptionTokens)], terms, scores);

			var results = new List<AddonSearchResult<T>>();
			for (int i = 0; i < documents.Count; i++)
			{
				if (scores[i] <= 0)
					continue;

				results.Add(new AddonSearchResult<T>
				{
					Addon = documents[i].Addon,
					Score = scores[i]
				});
			}

			var ordered = results
				.OrderByDescending(result => result.Score)
				.ThenBy(result => result.Addon.Name, StringComparer.OrdinalIgnoreCase)
				.ToList();

			if (maxResults > 0 && ordered.Count > maxResults)
				ordered.RemoveRange(maxResults, ordered.Count - maxResults);

			return ordered;
		}

		/// <summary>
		/// Scores one field of every document with the BM25 formula and adds the scores to <paramref name="scores"/>.
		/// </summary>
		private static void ScoreField(double weight, List<List<string>> tokens, IReadOnlyList<string> terms,
			double[] scores)
		{
			// Token postings: token -> (document index -> term frequency).
			var documentFrequency = new Dictionary<string, int>(StringComparer.Ordinal);
			var postings = new Dictionary<string, Dictionary<int, int>>(StringComparer.Ordinal);

			for (int i = 0; i < tokens.Count; i++)
			{
				foreach (var group in tokens[i].GroupBy(token => token, StringComparer.Ordinal))
				{
					if (!postings.TryGetValue(group.Key, out var documentPostings))
					{
						documentPostings = new Dictionary<int, int>();
						postings[group.Key] = documentPostings;
						documentFrequency[group.Key] = 0;
					}

					documentPostings[i] = group.Count();
					documentFrequency[group.Key]++;
				}
			}

			var averageLength = tokens.Count == 0 ? 0.0 : tokens.Average(tokenList => tokenList.Count);

			foreach (var term in terms)
			{
				foreach (var (token, documentPostings) in postings)
				{
					if (!token.StartsWith(term, StringComparison.Ordinal))
						continue;

					var frequency = documentFrequency[token];
					var idf = Math.Log(1 + (tokens.Count - frequency + 0.5) / (frequency + 0.5));

					foreach (var (index, termFrequency) in documentPostings)
					{
						var normalization = averageLength <= 0 ? 0 : tokens[index].Count / averageLength;
						var termScore = idf * (termFrequency * (Bm25K1 + 1))
							/ (termFrequency + Bm25K1 * (1 - Bm25B + Bm25B * normalization));

						scores[index] += weight * termScore;
					}
				}
			}
		}

		/// <summary>
		/// Splits the specified text into lowercase tokens: a token is a sequence of letters and digits,
		/// which keeps both Latin and Cyrillic text searchable.
		/// </summary>
		private static List<string> Tokenize(string? text)
		{
			if (string.IsNullOrEmpty(text))
				return [];

			var tokens = new List<string>();
			var builder = new StringBuilder();

			foreach (var character in text)
			{
				if (char.IsLetterOrDigit(character))
				{
					builder.Append(char.ToLowerInvariant(character));
				}
				else if (builder.Length > 0)
				{
					tokens.Add(builder.ToString());
					builder.Clear();
				}
			}

			if (builder.Length > 0)
				tokens.Add(builder.ToString());

			return tokens;
		}

		private sealed class Document
		{
			public Document(T addon, string query)
			{
				Addon = addon;
				NameTokens = Tokenize(addon.Name);
				TagTokens = [.. addon.Tags.SelectMany(Tokenize)];
				DescriptionTokens = Tokenize(addon.Description);

				if (addon.Name.Equals(query, StringComparison.OrdinalIgnoreCase))
					Bonus = ExactNameBonus;
			}

			public T Addon { get; }

			public List<string> NameTokens { get; }

			public List<string> TagTokens { get; }

			public List<string> DescriptionTokens { get; }

			public double Bonus { get; } = 0;
		}
	}
}
