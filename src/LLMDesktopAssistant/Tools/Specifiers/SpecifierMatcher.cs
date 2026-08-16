using System.Collections.Frozen;

namespace LLMDesktopAssistant.Tools.Specifiers
{
	/// <summary>
	/// Matches <see cref="Specifier"/> instances against the parts of the main tool argument
	/// and the named tool parameters.
	/// </summary>
	public static class SpecifierMatcher
	{
		/// <summary>
		/// Matches the specifier against the provided argument parts and parameters.
		/// </summary>
		/// <param name="specifier">The specifier to match. Cannot be <see langword="null"/>.</param>
		/// <param name="mainArgumentParts">The parts of the main argument (for example, individual commands of a compound shell command).</param>
		/// <param name="parameters">The named tool parameters (parameter name → string value).</param>
		/// <returns>
		/// <see cref="SpecifierMatchResult.FullMatch"/> when every target of the matched groups
		/// (the main argument parts and the values of the referenced parameters) is covered,
		/// <see cref="SpecifierMatchResult.PartialMatch"/> when only some targets are covered,
		/// and <see cref="SpecifierMatchResult.NoMatch"/> when no group of the specifier matches anything.
		/// </returns>
		public static SpecifierMatchResult Match(Specifier specifier,
			IEnumerable<string> mainArgumentParts, IEnumerable<KeyValuePair<string, string>> parameters)
		{
			ArgumentNullException.ThrowIfNull(specifier);
			ArgumentNullException.ThrowIfNull(mainArgumentParts);
			ArgumentNullException.ThrowIfNull(parameters);

			var parts = mainArgumentParts.ToHashSet(StringComparer.Ordinal);
			var parameterValues = parameters
				.GroupBy(p => p.Key, StringComparer.Ordinal)
				.ToFrozenDictionary(g => g.Key, g => g.First().Value, StringComparer.Ordinal);

			HashSet<string>? targets = null;
			HashSet<string>? covered = null;

			foreach (var group in specifier.Parts)
			{
				var (matched, groupTargets, groupCovered) = MatchGroup(group, parts, parameterValues);
				if (!matched)
					continue;

				targets ??= new HashSet<string>(StringComparer.Ordinal);
				covered ??= new HashSet<string>(StringComparer.Ordinal);
				targets.UnionWith(groupTargets);
				covered.UnionWith(groupCovered);
			}

			if (targets == null)
				return SpecifierMatchResult.NoMatch;

			return covered!.IsSupersetOf(targets) ? SpecifierMatchResult.FullMatch : SpecifierMatchResult.PartialMatch;
		}

		/// <summary>
		/// Matches a single group (an AND-group or a standalone literal) and returns whether it matched,
		/// its targets and its covered targets.
		/// </summary>
		private static (bool Matched, IEnumerable<string> Targets, IEnumerable<string> Covered) MatchGroup(
			SpecifierPart group, IReadOnlySet<string> parts, FrozenDictionary<string, string> parameterValues)
		{
			if (group is SpecifierAndPart andPart)
			{
				var targets = new HashSet<string>(StringComparer.Ordinal);
				var covered = new HashSet<string>(StringComparer.Ordinal);

				foreach (var literal1 in andPart.Parts)
				{
					var (literalTargets, literalCovered) = MatchLiteral(literal1, parts, parameterValues);

					// AND semantics: every literal must cover ALL of its targets, because
					// all literals describe the same object (x == 1 && x == 2 is impossible).
					if (literalTargets.Count == 0 || !literalCovered.IsSupersetOf(literalTargets))
						return (false, targets, covered);

					targets.UnionWith(literalTargets);
					covered.UnionWith(literalCovered);
				}

				return (true, targets, covered);
			}

			if (group is SpecifierLiteralPart literal2)
			{
				var (targets, covered) = MatchLiteral(literal2, parts, parameterValues);
				return (covered.Count > 0, targets, covered);
			}

			return (false, [], []);
		}

		/// <summary>
		/// Matches a single literal against its targets: the value of the referenced parameter
		/// for <see cref="SpecifierParameterPart"/>, or all main argument parts otherwise.
		/// </summary>
		private static (HashSet<string> Targets, HashSet<string> Covered) MatchLiteral(
			SpecifierLiteralPart literal, IReadOnlySet<string> parts, FrozenDictionary<string, string> parameterValues)
		{
			if (literal is SpecifierParameterPart parameterPart)
			{
				var targets = new HashSet<string>(StringComparer.Ordinal);
				var covered = new HashSet<string>(StringComparer.Ordinal);

				if (parameterValues.TryGetValue(parameterPart.Name, out var value))
				{
					targets.Add(value);
					if (GlobMatch(parameterPart.Value, value))
						covered.Add(value);
				}

				return (targets, covered);
			}

			var coveredParts = new HashSet<string>(StringComparer.Ordinal);
			foreach (var part in parts)
			{
				if (GlobMatch(literal.Value, part))
					coveredParts.Add(part);
			}

			return (new HashSet<string>(parts, StringComparer.Ordinal), coveredParts);
		}

		/// <summary>
		/// Matches a glob pattern against a text: <c>*</c> matches any sequence of characters
		/// (including an empty one), <c>?</c> matches exactly one character and the <c>:*</c> suffix
		/// matches either the end of the text or a space followed by anything (any arguments, including none).
		/// Matching is case-sensitive.
		/// </summary>
		private static bool GlobMatch(string pattern, string text)
		{
			return GlobMatch(pattern.AsSpan(), text.AsSpan());
		}

		private static bool GlobMatch(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text)
		{
			int pi = 0;
			int ti = 0;

			while (pi < pattern.Length)
			{
				switch (pattern[pi])
				{
					case '*':
						while (pi < pattern.Length && pattern[pi] == '*')
							pi++;
						if (pi == pattern.Length)
							return true;
						for (int i = ti; i <= text.Length; i++)
						{
							if (GlobMatch(pattern[pi..], text[i..]))
								return true;
						}
						return false;

					case '?':
						if (ti >= text.Length)
							return false;
						pi++;
						ti++;
						break;

					case ':':
						if (pi + 1 < pattern.Length && pattern[pi + 1] == '*')
						{
							// ":*" matches either the end of the text or a space followed by anything.
							if (ti == text.Length)
								return true;
							if (text[ti] != ' ')
								return false;
							pi++;
							ti++;
							break;
						}
						if (ti >= text.Length || pattern[pi] != text[ti])
							return false;
						pi++;
						ti++;
						break;

					default:
						if (ti >= text.Length || pattern[pi] != text[ti])
							return false;
						pi++;
						ti++;
						break;
				}
			}

			return ti == text.Length;
		}
	}
}
