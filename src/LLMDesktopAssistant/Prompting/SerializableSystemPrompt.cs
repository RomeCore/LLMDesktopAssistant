using System.Diagnostics.CodeAnalysis;
using RCLargeLanguageModels;

namespace LLMDesktopAssistant.Prompting
{
	public readonly struct SerializableSystemPrompt : IEquatable<SerializableSystemPrompt>
	{
		/// <summary>
		/// The textual content of the system prompt.
		/// </summary>
		public required string Text { get; init; }

		/// <summary>
		/// A collection of tools that is injected into the system prompt by the provider.
		/// </summary>
		public required ImmutableArray<SerializableToolDefinition> Tools { get; init; }

		public bool Equals(SerializableSystemPrompt other)
		{
			return Text == other.Text && Tools.SequenceEqual(other.Tools);
		}

		public override bool Equals([NotNullWhen(true)] object? obj)
		{
			return obj is SerializableSystemPrompt other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Text, Tools.GetSequenceHashCode());
		}

		public static bool operator ==(SerializableSystemPrompt left, SerializableSystemPrompt right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(SerializableSystemPrompt left, SerializableSystemPrompt right)
		{
			return !(left == right);
		}
	}
}
