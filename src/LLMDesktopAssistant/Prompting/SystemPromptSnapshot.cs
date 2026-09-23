using System.Diagnostics.CodeAnalysis;
using RCLargeLanguageModels;

namespace LLMDesktopAssistant.Prompting
{
	public class SystemPromptSnapshot : IEquatable<SystemPromptSnapshot>
	{
		/// <summary>
		/// The textual content of the system prompt.
		/// </summary>
		public string Text { get; init; } = string.Empty;

		/// <summary>
		/// A collection of tools that is injected into the system prompt by the provider.
		/// </summary>
		public IReadOnlyList<SerializableToolDefinition> Tools { get; init; } = [];

		public static implicit operator SystemPromptSnapshot(string text) => new()
		{
			Text = text,
			Tools = []
		};

		public bool Equals(SystemPromptSnapshot? other)
		{
			if (other is null)
				return false;
			return Text == other.Text && Tools.SequenceEqual(other.Tools);
		}

		public override bool Equals([NotNullWhen(true)] object? obj)
		{
			return obj is SystemPromptSnapshot other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Text, Tools.GetSequenceHashCode());
		}

		public static bool operator ==(SystemPromptSnapshot left, SystemPromptSnapshot right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(SystemPromptSnapshot left, SystemPromptSnapshot right)
		{
			return !(left == right);
		}
	}
}
