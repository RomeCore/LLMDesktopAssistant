namespace LLMDesktopAssistant.Tools.Specifiers
{
	/// <summary>
	/// A single part of a <see cref="Specifier"/> expression: either a standalone literal,
	/// a parameter-bound literal, or an AND-group of literals.
	/// </summary>
	public abstract class SpecifierPart
	{
	}

	/// <summary>
	/// A literal pattern that is matched against the parts of the main tool argument.
	/// Supports <c>*</c> (any sequence of characters) and <c>?</c> (exactly one character) wildcards.
	/// </summary>
	public class SpecifierLiteralPart : SpecifierPart
	{
		/// <summary>
		/// The literal pattern value.
		/// </summary>
		public required string Value { get; init; }
	}

	/// <summary>
	/// A literal pattern bound to a named tool parameter (<c>name:value</c> syntax).
	/// Matched against the value of the parameter with the specified <see cref="Name"/>.
	/// </summary>
	public class SpecifierParameterPart : SpecifierLiteralPart
	{
		/// <summary>
		/// The name of the tool parameter this part is matched against.
		/// </summary>
		public required string Name { get; init; }
	}

	/// <summary>
	/// An AND-group of literals (<c>literal1 &amp;&amp; literal2</c> syntax).
	/// The group matches only when every literal matches at least one of its targets.
	/// </summary>
	public class SpecifierAndPart : SpecifierPart
	{
		/// <summary>
		/// The literals that must all match for the group to match.
		/// </summary>
		public required ImmutableList<SpecifierLiteralPart> Parts { get; init; }
	}

	/// <summary>
	/// A parsed specifier expression: an OR-list of groups (<c>group1 || group2</c> syntax).
	/// The specifier matches when at least one of its groups matches.
	/// </summary>
	public class Specifier
	{
		/// <summary>
		/// The OR-groups of the specifier.
		/// </summary>
		public required ImmutableList<SpecifierPart> Parts { get; init; }

		/// <summary>
		/// Combines multiple specifiers into a single one by concatenating their parts.
		/// The resulting specifier matches when any of the combined specifiers matches.
		/// </summary>
		/// <param name="specifiers">The specifiers to combine. Cannot be <see langword="null"/>.</param>
		/// <returns>A new specifier whose parts are the concatenation of the input parts.</returns>
		public static Specifier Combined(params Specifier[] specifiers)
		{
			return new Specifier
			{
				Parts = [.. specifiers.SelectMany(s => s.Parts)]
			};
		}

		/// <summary>
		/// Combines multiple specifiers into a single one by concatenating their parts.
		/// The resulting specifier matches when any of the combined specifiers matches.
		/// </summary>
		/// <param name="specifiers">The specifiers to combine. Cannot be <see langword="null"/>.</param>
		/// <returns>A new specifier whose parts are the concatenation of the input parts.</returns>
		public static Specifier Combined(IEnumerable<Specifier> specifiers)
		{
			return new Specifier
			{
				Parts = [.. specifiers.SelectMany(s => s.Parts)]
			};
		}
	}
}
