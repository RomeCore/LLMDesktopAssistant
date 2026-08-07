using LiteDB;

namespace LLMDesktopAssistant.Data.MemoryModels
{
	public class MemoryFactModel
	{
		[BsonId]
		public int Id { get; set; }

		/// <summary>
		/// The date and time when the fact was created in the UTC timezone.
		/// </summary>
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		/// <summary>
		/// The date and time when the fact was last updated in the UTC timezone.
		/// </summary>
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

		/// <summary>
		/// The date and time when the fact was last accessed in the UTC timezone.
		/// Null when the fact has never been accessed.
		/// </summary>
		public DateTime? LastAccessedAt { get; set; } = null;

		/// <summary>
		/// The number of times the fact has been accessed.
		/// </summary>
		public int AccessCount { get; set; } = 0;



		/// <summary>
		/// The ID of the chat where the fact was created.
		/// </summary>
		public int SourceChatId { get; set; }

		/// <summary>
		/// The ID of the message that this fact is associated with.
		/// </summary>
		public int SourceMessageId { get; set; }



		/// <summary>
		/// The text of the fact.
		/// </summary>
		public string Text { get; set; } = string.Empty;

		/// <summary>
		/// The status of the fact.
		/// </summary>
		public MemoryFactStatus Status { get; set; } = MemoryFactStatus.Active;

		/// <summary>
		/// The ID of the fact by which this fact has been superseded.
		/// Zero when the fact is not superseded.
		/// </summary>
		public int SupersededBy { get; set; }

		/// <summary>
		/// The importance score of the fact, which is a value between 0 and 1.0.
		/// </summary>
		public double Importance { get; set; } = 1.0;

		/// <summary>
		/// The number of tokens in the text of the fact for the BM25 search.
		/// </summary>
		public int TokenCount { get; set; } = 0;
	}
}
