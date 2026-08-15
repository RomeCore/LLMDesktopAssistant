using LLMDesktopAssistant.Data.MemoryModels;

namespace LLMDesktopAssistant.Agents.Memory
{
	/// <summary>
	/// Represents a lightweight result of a memory log store operation.
	/// </summary>
	public class MemoryLogResult
	{
		/// <summary>
		/// Gets the identifier of the log.
		/// </summary>
		public required int Id { get; init; }

		/// <summary>
		/// Gets the text of the log.
		/// </summary>
		public required string Text { get; init; }

		/// <summary>
		/// Gets the status of the log.
		/// </summary>
		public required MemoryLogStatus Status { get; init; }

		/// <summary>
		/// Gets the date and time when the log was created in the local timezone.
		/// </summary>
		public required DateTime CreatedAt { get; init; }

		/// <summary>
		/// Gets the real-time timestamp when the log began in the local timezone.
		/// </summary>
		public required DateTime TimeStampBegin { get; init; }

		/// <summary>
		/// Gets the real-time timestamp when the log ended in the local timezone.
		/// </summary>
		public required DateTime TimeStampEnd { get; init; }

		/// <summary>
		/// Gets the game-time ordinal when the log began (for example, the day number).
		/// </summary>
		public required double TimeLineOrdinalBegin { get; init; }

		/// <summary>
		/// Gets the game-time details when the log began (for example, "Day 3, 14:00").
		/// </summary>
		public required string TimeLineDetailsBegin { get; init; }

		/// <summary>
		/// Gets the game-time ordinal when the log ended.
		/// </summary>
		public required double TimeLineOrdinalEnd { get; init; }

		/// <summary>
		/// Gets the game-time details when the log ended.
		/// </summary>
		public required string TimeLineDetailsEnd { get; init; }

		/// <summary>
		/// Gets the ID of the chat where the log was created.
		/// </summary>
		public required int SourceChatId { get; init; }

		/// <summary>
		/// Gets the ID of the message that the log is associated with.
		/// </summary>
		public required int SourceMessageId { get; init; }

		/// <summary>
		/// Gets the importance score of the log, which is a value between 0 and 1.0.
		/// </summary>
		public required double Importance { get; init; }

		/// <summary>
		/// Gets the BM25 score of the log against a query.
		/// Null when the log was not produced by a text search.
		/// </summary>
		public required double? Bm25Score { get; init; }
	}
}
