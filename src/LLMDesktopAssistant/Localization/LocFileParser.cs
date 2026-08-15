using RCParsing;

namespace LLMDesktopAssistant.Localization
{
	/// <summary>
	/// Represents a parsed .loc file: its locale, optional namespace and the localized entries.
	/// </summary>
	public sealed class LocFileDocument
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LocFileDocument"/> class.
		/// </summary>
		/// <param name="locale">The locale code of the document (for example, "ru-RU" or "" for the neutral locale).</param>
		/// <param name="namespace">The namespace that prefixes all entry keys of the document, or <see langword="null"/>.</param>
		/// <param name="entries">The full keys mapped to their localized values.</param>
		public LocFileDocument(string locale, string? @namespace, IReadOnlyDictionary<string, string> entries)
		{
			Locale = locale;
			Namespace = @namespace;
			Entries = entries;
		}

		/// <summary>
		/// Gets the locale code of the document.
		/// </summary>
		public string Locale { get; }

		/// <summary>
		/// Gets the namespace that prefixes all entry keys of the document, or <see langword="null"/>.
		/// </summary>
		public string? Namespace { get; }

		/// <summary>
		/// Gets the full keys mapped to their localized values.
		/// </summary>
		public IReadOnlyDictionary<string, string> Entries { get; }
	}

	/// <summary>
	/// The exception that is thrown when a .loc file cannot be parsed or violates the format rules.
	/// </summary>
	public sealed class LocFileParseException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LocFileParseException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		public LocFileParseException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LocFileParseException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="innerException">The inner exception.</param>
		public LocFileParseException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}

	/// <summary>
	/// Parses .loc localization files. The format supports metadata lines (<c>%key: value</c>),
	/// single-line entries (<c>key: value</c>), multiline entries (<c>key """ ... """</c>) and
	/// full-line comments (<c>// comment</c>).
	/// </summary>
	public static class LocFileParser
	{
		private static readonly Parser _parser;

		static LocFileParser()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("key")
				.Identifier(
					static c => char.IsLetter(c) || c is '_' or '-',
					static c => char.IsLetterOrDigit(c) || c is '_' or '-' or '.',
					minLength: 1)
				.Transform(v => v.Text);

			builder.CreateRule("value")
				.TextUntil('\n', '\r')
				.Transform(v => v.GetIntermediateValue<string>().Trim());

			builder.CreateRule("single_entry")
				.Rule("key").Label("key")
				.Optional(b => b.Spaces())
				.Literal(":")
				.Optional(b => b.Spaces())
				.Rule("value").Label("value")
				.Optional(b => b.Newline())
				.Transform(v => new LocFileItem(LocFileItemKind.Entry, v["key"].GetValue<string>(), v["value"].GetIntermediateValue<string>().Trim()));

			builder.CreateRule("multiline_entry")
				.Rule("key").Label("key")
				.Optional(b => b.Spaces())
				.Literal("\"\"\"")
				.Newline()
				.TextUntil("\"\"\"").Label("value")
				.Optional(b => b.Newline())
				.Literal("\"\"\"")
				.Optional(b => b.Newline())
				.Transform(v =>
				{
					var key = v["key"].GetValue<string>();
					var value = NormalizeMultiline(v["value"].GetIntermediateValue<string>());
					return new LocFileItem(LocFileItemKind.Entry, key, value);
				});

			builder.CreateRule("metadata_entry")
				.Literal("%")
				.Rule("key").Label("key")
				.Optional(b => b.Spaces())
				.Literal(":")
				.Rule("value").Label("value")
				.Optional(b => b.Newline())
				.Transform(v => new LocFileItem(LocFileItemKind.Metadata, v["key"].GetValue<string>(), v["value"].GetIntermediateValue<string>().Trim()));

			builder.CreateRule("comment_line")
				.Literal("//")
				.Optional(b => b.TextUntil('\n', '\r'))
				.Optional(b => b.Newline())
				.Transform(v => (LocFileItem?)null);

			builder.CreateRule("empty_line")
				.Newline()
				.Transform(v => (LocFileItem?)null);

			builder.CreateRule("line")
				.Choice(
					b => b.Rule("multiline_entry"),
					b => b.Rule("metadata_entry"),
					b => b.Rule("single_entry"),
					b => b.Rule("comment_line"),
					b => b.Rule("empty_line"));

			builder.CreateMainRule()
				.ZeroOrMore(b => b.Rule("line"))
				.EOF()
				.Transform(v => BuildDocument(v[0].Select(v => v.TryGetValue<LocFileItem>())));

			_parser = builder.Build();
		}

		/// <summary>
		/// Parses the content of a .loc file into a <see cref="LocFileDocument"/>.
		/// </summary>
		/// <param name="content">The raw content of the .loc file.</param>
		/// <returns>The parsed document.</returns>
		/// <exception cref="LocFileParseException">Thrown when the content is malformed or violates the format rules.</exception>
		public static LocFileDocument Parse(string content)
		{
			try
			{
				return _parser.Parse<LocFileDocument>(content);
			}
			catch (LocFileParseException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new LocFileParseException($"Failed to parse .loc file: {ex.Message}", ex);
			}
		}

		private static LocFileDocument BuildDocument(IEnumerable<LocFileItem?> items)
		{
			var nonNullItems = items.Where(i => i != null).Select(i => i!).ToArray();

			var metadata = nonNullItems
				.Where(i => i.Kind == LocFileItemKind.Metadata)
				.ToDictionary(i => i.Key, i => i.Value ?? string.Empty, StringComparer.Ordinal);

			if (!metadata.TryGetValue("locale", out var locale))
				throw new LocFileParseException("Missing required metadata '%locale'.");

			var @namespace = metadata.GetValueOrDefault("namespace");

			var entries = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (var item in nonNullItems.Where(i => i.Kind == LocFileItemKind.Entry))
			{
				var fullKey = string.IsNullOrEmpty(@namespace) ? item.Key : $"{@namespace}.{item.Key}";
				if (!entries.TryAdd(fullKey, item.Value ?? string.Empty))
					throw new LocFileParseException($"Duplicate key '{fullKey}'.");
			}

			return new LocFileDocument(locale, @namespace, entries);
		}

		private static string NormalizeMultiline(string raw)
		{
			var lines = raw.Replace("\r\n", "\n").Split('\n').ToList();

			// The first line is always the newline right after the opening """.
			if (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
				lines.RemoveAt(0);

			// Trailing empty lines before the closing """ are not part of the value.
			while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
				lines.RemoveAt(lines.Count - 1);

			var nonEmptyLines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
			if (nonEmptyLines.Count == 0)
				return string.Empty;

			var indent = nonEmptyLines.Min(l => l.TakeWhile(char.IsWhiteSpace).Count());
			return string.Join('\n', lines.Select(l => l.Length >= indent ? l[indent..] : l.TrimStart()));
		}

		private enum LocFileItemKind
		{
			Entry,
			Metadata
		}

		private sealed record LocFileItem(LocFileItemKind Kind, string Key, string? Value);
	}
}
