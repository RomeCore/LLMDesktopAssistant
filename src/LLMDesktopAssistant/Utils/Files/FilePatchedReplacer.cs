using System.Text;

namespace LLMDesktopAssistant.Utils.Files;

/// <summary>
/// Applies plain-text replacement patches to file content with flexible whitespace handling.
/// Matching ignores leading/trailing whitespace and common indentation; the replacement is
/// re-indented to the file's own indentation, measured in columns with a configurable tab size.
/// </summary>
public static class FilePatchedReplacer
{
	/// <summary>
	/// Replaces all occurrences of <paramref name="match"/> with <paramref name="replace"/> in
	/// <paramref name="content"/>, preserving the file's indentation and any text surrounding
	/// the match on the matched lines.
	/// </summary>
	/// <param name="content">The file content. Line endings are normalized internally.</param>
	/// <param name="match">The literal text to search for (can span multiple lines).</param>
	/// <param name="replace">The replacement text. An empty string deletes the match.</param>
	/// <param name="tabSize">The number of columns a tab character occupies when measuring indentation.</param>
	/// <param name="ignoreCase">Whether matching is case-insensitive.</param>
	/// <returns>The modified content, or <see langword="null"/> if no occurrence was found.</returns>
	public static string? Replace(string content, string match, string replace, int tabSize = 4, bool ignoreCase = false)
	{
		var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		var matchLines = SplitLines(match);
		var replaceLines = SplitLines(replace);
		if (matchLines.Count == 0)
			return null;

		var fileLines = Normalize(content).Split('\n').ToList();

		if (matchLines.Count == 1)
			return ReplaceSingleLine(fileLines, matchLines[0].Trim(), replaceLines, comparison);

		return ReplaceBlock(fileLines, matchLines, replaceLines, tabSize, comparison);
	}

	private static string? ReplaceSingleLine(
		List<string> fileLines, string match, List<string> replaceLines, StringComparison comparison)
	{
		var foundAny = false;
		for (int i = 0; i < fileLines.Count; i++)
		{
			var newLine = ReplaceOccurrencesInLine(fileLines[i], match, replaceLines, comparison);
			if (newLine != fileLines[i])
			{
				foundAny = true;
				fileLines[i] = newLine;
			}
		}

		return foundAny ? string.Join('\n', fileLines) : null;
	}

	private static string ReplaceOccurrencesInLine(
		string line, string match, List<string> replacementLines, StringComparison comparison)
	{
		var result = new StringBuilder();
		int pos = 0;
		while (true)
		{
			int index = line.IndexOf(match, pos, comparison);
			if (index < 0)
				break;

			result.Append(line, pos, index - pos);

			var prefix = line[pos..index];
			var firstLine = replacementLines.Count > 0 ? replacementLines[0] : "";
			// If the text before the match is pure indentation, keep the file's indentation
			// and strip the replacement's own leading whitespace to avoid doubling it
			result.Append(IsIndentation(prefix) ? firstLine.TrimStart() : firstLine);

			for (int k = 1; k < replacementLines.Count; k++)
			{
				result.Append('\n');
				result.Append(replacementLines[k]);
			}

			pos = index + match.Length;
		}

		if (pos == 0)
			return line;

		result.Append(line, pos, line.Length - pos);
		return result.ToString();
	}

	private static string? ReplaceBlock(
		List<string> fileLines, List<string> matchLines, List<string> replaceLines, int tabSize, StringComparison comparison)
	{
		if (fileLines.Count < matchLines.Count)
			return null;

		var matchTrimmed = matchLines.Select(l => l.Trim()).ToArray();
		var matchClean = CalculateIndents(matchLines, tabSize, out var matchMin);
		var replaceClean = CalculateIndents(replaceLines, tabSize, out var replaceMin);
		var columnMin = Math.Min(matchMin, replaceMin);
		CleanIndents(matchLines, tabSize, matchClean, columnMin);
		CleanIndents(replaceLines, tabSize, replaceClean, columnMin);
		var foundAny = false;

		for (int i = 0; i <= fileLines.Count - matchLines.Count; i++)
		{
			var prefixes = new string[matchLines.Count];
			var suffixes = new string[matchLines.Count];
			var blockMatched = true;
			for (int j = 0; j < matchLines.Count; j++)
			{
				var line = fileLines[i + j];
				var index = line.IndexOf(matchTrimmed[j], comparison);
				if (index < 0)
				{
					blockMatched = false;
					break;
				}

				prefixes[j] = line[..index];
				suffixes[j] = line[(index + matchTrimmed[j].Length)..];

				// The match must be aligned with the line: non-whitespace text on both
				// sides of the match would make the replacement position ambiguous
				if (HasNonWhitespace(prefixes[j]) && HasNonWhitespace(suffixes[j]))
				{
					blockMatched = false;
					break;
				}
			}

			if (!blockMatched)
				continue;

			foundAny = true;

			List<string> newBlockLines;
			if (replaceLines.Count == 0)
			{
				// Delete: keep the text before the match (first line) and after it (last line)
				newBlockLines = [prefixes[0] + suffixes[^1]];
			}
			else
			{
				newBlockLines = BuildReplacement(fileLines, i, matchLines, replaceLines, matchClean, replaceClean, prefixes, suffixes, tabSize);
			}

			fileLines.RemoveRange(i, matchLines.Count);
			fileLines.InsertRange(i, newBlockLines);

			// Continue scanning after the replaced block; the for-loop's i++ moves past it
			i += newBlockLines.Count - 1;
		}

		return foundAny ? string.Join('\n', fileLines) : null;
	}

	private static List<string> BuildReplacement(
		List<string> fileLines, int start, List<string> matchLines, List<string> replaceLines,
		int[] matchClean, int[] replaceClean, string[] prefixes, string[] suffixes, int tabSize)
	{
		var hasNonWhitespacePrefix = HasNonWhitespace(prefixes[0]);
		// The vertical offset between the file's and the match's base indentation, measured
		// on the first line. When it is zero the file and the patch use the same indentation
		// and the replacement lines keep their own whitespace; otherwise they are re-aligned
		// to the file line by line.
		var shift = GetIndentation(fileLines[start], tabSize) - matchClean[0];
		var result = new List<string>(replaceLines.Count);

		for (int j = 0; j < replaceLines.Count; j++)
		{
			string line;
			if (string.IsNullOrWhiteSpace(replaceLines[j]))
			{
				line = "";
			}
			else if (j == 0 && hasNonWhitespacePrefix)
			{
				// A non-whitespace prefix (text before the match on the first line) is kept
				// as-is and the first replacement line follows it without any indentation
				line = prefixes[0] + replaceLines[j].TrimStart();
			}
			else if (shift == 0)
			{
				line = replaceLines[j];
			}
			else
			{
				// Align each replacement line with the file: the file's indentation at the
				// corresponding matched line plus the replacement's own depth relative to
				// the match. The last replacement line is aligned with the last matched line
				// (it usually closes the block); extra lines follow it too. Measured in
				// columns so tabs and spaces can be mixed freely.
				var mapped = j == replaceLines.Count - 1 ? matchLines.Count - 1 : Math.Min(j, matchLines.Count - 1);
				string styleLine;
				int targetColumns;
				if (string.IsNullOrWhiteSpace(matchLines[mapped]))
				{
					// A blank matched line carries no indentation of its own; inherit the
					// indentation of the previous replacement line instead
					var prev = result.Count - 1;
					while (prev >= 0 && string.IsNullOrWhiteSpace(result[prev]))
						prev--;
					styleLine = prev >= 0 ? result[prev] : fileLines[start + mapped];
					targetColumns = GetIndentation(styleLine, tabSize);
				}
				else
				{
					styleLine = fileLines[start + mapped];
					targetColumns = replaceClean[j] + GetIndentation(styleLine, tabSize) - matchClean[mapped];
				}
				line = MakeIndent(targetColumns, styleLine, tabSize) + replaceLines[j].TrimStart();
			}

			// Text outside the match on the matched lines (e.g. a trailing comment)
			// is never part of the match and must not be deleted; whitespace-only
			// tails (e.g. the indentation of a blank matched line) are dropped
			if (j == replaceLines.Count - 1)
				line += suffixes[^1];
			else if (j < suffixes.Length - 1 && HasNonWhitespace(suffixes[j]))
				line += suffixes[j];

			result.Add(line);
		}

		return result;
	}

	private static List<string> SplitLines(string text)
	{
		var lines = Normalize(text).Split('\n').ToList();
		if (lines.Count > 0 && string.IsNullOrEmpty(lines[^1]))
			lines.RemoveAt(lines.Count - 1);
		return lines;
	}

	private static string Normalize(string text)
		=> text.Replace("\r\n", "\n").Replace("\r", "\n");

	private static bool IsIndentation(string text)
		=> text.Length > 0 && text.All(c => c is ' ' or '\t');

	private static bool HasNonWhitespace(string text)
		=> text.Any(c => c is not ' ' and not '\t');


	private static int[] CalculateIndents(List<string> lines, int tabSize, out int min)
	{
		min = int.MaxValue;

		var columns = new int[lines.Count];
		for (int i = 0; i < lines.Count; i++)
		{
			columns[i] = GetIndentation(lines[i], tabSize);
			if (!string.IsNullOrWhiteSpace(lines[i]))
				min = Math.Min(min, columns[i]);
		}

		if (min == int.MaxValue)
			min = 0;

		return columns;
	}

	/// <summary>
	/// Computes each line's indentation relative to the minimum indentation among non-blank
	/// lines, in columns. Blank lines do not affect the minimum.
	/// </summary>
	private static void CleanIndents(List<string> lines, int tabSize, int[] columns, int min)
	{
		for (int i = 0; i < columns.Length; i++)
			columns[i] = Math.Max(0, columns[i] - min);
	}

	private static int GetIndentation(string line, int tabSize)
	{
		var columns = 0;
		foreach (var c in line)
		{
			if (c == ' ')
				columns++;
			else if (c == '\t')
				columns += tabSize - (columns % tabSize);
			else
				break;
		}

		return columns;
	}

	private static string MakeIndent(int columns, string fileLine, int tabSize)
	{
		if (columns <= 0)
			return "";
		if (UsesTabs(fileLine))
			return new string('\t', columns / tabSize) + new string(' ', columns % tabSize);
		return new string(' ', columns);
	}

	private static bool UsesTabs(string line)
	{
		foreach (var c in line)
		{
			if (c == '\t')
				return true;
			if (c != ' ')
				return false;
		}

		return false;
	}
}
