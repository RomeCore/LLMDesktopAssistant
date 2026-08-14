using LLMDesktopAssistant.Data.MemoryModels;

namespace LLMDesktopAssistant.Agents.Memory
{
	/// <summary>
	/// Represents a lightweight result of a memory store operation.
	/// </summary>
	public class MemoryFactResult
	{
		/// <summary>
		/// Gets the identifier of the fact.
		/// </summary>
		public required int Id { get; init; }

		/// <summary>
		/// Gets the text of the fact.
		/// </summary>
		public required string Text { get; init; }

		/// <summary>
		/// Gets the status of the fact.
		/// </summary>
		public required MemoryFactStatus Status { get; init; }

		/// <summary>
		/// Gets the date and time when the fact was created in the local timezone.
		/// </summary>
		public required DateTime CreatedAt { get; init; }

		/// <summary>
		/// Gets the date and time when the fact was last updated in the local timezone.
		/// </summary>
		public required DateTime UpdatedAt { get; init; }

		/// <summary>
		/// Gets the date and time when the fact was last accessed in the local timezone.
		/// Null when the fact has never been accessed.
		/// </summary>
		public required DateTime? LastAccessedAt { get; init; }

		/// <summary>
		/// Gets the number of times the fact has been accessed.
		/// </summary>
		public required int AccessCount { get; init; }

		/// <summary>
		/// Gets the importance score of the fact, which is a value between 0 and 1.0.
		/// </summary>
		public required double Importance { get; init; }

		/// <summary>
		/// Gets the cosine similarity (embedding) score of the fact against a query.
		/// </summary>
		public required double? CosineScore { get; init; }

		/// <summary>
		/// Gets the BM25 score of the fact against a query.
		/// </summary>
		public required double? Bm25Score { get; init; }

		/// <summary>
		/// Gets the reciprocal rank fusion score of the fact, which determines its position
		/// in the result set. The score is relative: it is only meaningful for comparing
		/// facts within the same query's results and cannot be used as an absolute threshold.
		/// Null when the fact was not produced by a search (for example, when it was just stored).
		/// </summary>
		public required double? RrfScore { get; init; }
	}
}
