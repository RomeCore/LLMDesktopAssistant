namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents information about a chat session. Used for display purposes or when saving/loading chat sessions.
	/// </summary>
	public class ChatInfo : IEquatable<ChatInfo>
	{
		/// <summary>
		/// Gets or sets the unique identifier for the chat session. Used mostly for database purposes.
		/// </summary>
		public required int Id { get; init; }

		/// <summary>
		/// Gets the title associated with the current instance.
		/// </summary>
		public required string Title { get; init; }

		/// <summary>
		/// Gets or sets the topic/category of the chat session.
		/// Human-readable category like "coding", "roleplay", "dnd", etc.
		/// </summary>
		public string Topic { get; init; } = string.Empty;

		/// <summary>
		/// Gets the creation timestamp for this instance. Used to track when the chat session was first created.
		/// </summary>
		public required DateTime CreatedAt { get; init; }

		/// <summary>
		/// Gets the last modified timestamp for this instance. Used to track when the chat session was last updated.
		/// </summary>
		public required DateTime LastModifiedAt { get; init; }



		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		public override bool Equals(object? obj)
		{
			return obj is ChatInfo chatInfo && Equals(chatInfo);
		}

		public bool Equals(ChatInfo? other)
		{
			return other != null && Id == other.Id;
		}



		/// <summary>
		/// Generates a deterministic pastel hex color from the topic string for UI display.
		/// Topic color is computed from hash, so any topic can be used without predefined list.
		/// Returns a hex color string like "#AABBCC" that can be used directly in XAML backgrounds.
		/// </summary>
		public string TopicColorHex => GenerateColorHexFromHash(Topic);

		private static string GenerateColorHexFromHash(string topic)
		{
			if (string.IsNullOrEmpty(topic))
				return "#808080";

			var hash = GetDeterministicHashCode(topic);

			const double min = 0.35;
			const double range = 0.65;

			double rh = (hash * 397 % 255) / 255.0;
			byte r2 = (byte)(min * 255 + rh * range * 255);
			double rg = (hash * 137 * 397 % 255) / 255.0;
			byte g2 = (byte)(min * 255 + rg * range * 255);
			double rb = (hash * 37 * 397 % 255) / 255.0;
			byte b2 = (byte)(min * 255 + rb * range * 255);

			return $"#{r2:X2}{g2:X2}{b2:X2}";
		}

		private static int GetDeterministicHashCode(string str)
		{
			unchecked
			{
				int hash1 = (5381 << 16) + 5381;
				int hash2 = hash1;

				for (int i = 0; i < str.Length; i += 2)
				{
					hash1 = ((hash1 << 5) + hash1) ^ str[i];
					if (i + 1 < str.Length)
						hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
				}

				return hash1 + hash2 * 1566083941;
			}
		}
	}
}