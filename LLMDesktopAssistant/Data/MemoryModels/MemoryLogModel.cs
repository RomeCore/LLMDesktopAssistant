using LiteDB;

namespace LLMDesktopAssistant.Data.MemoryModels
{
	public class MemoryLogModel
	{
		[BsonId]
		public int Id { get; set; }

		/// <summary>
		/// The date and time when the log was created in the local timezone.
		/// </summary>
		public DateTime CreatedAt { get; set; } = DateTime.Now;

		/// <summary>
		/// The date and time when the log was last updated in the local timezone.
		/// </summary>
		public DateTime UpdatedAt { get; set; } = DateTime.Now;



		/// <summary>
		/// The timestamp when the log began in the local timezone. This is used to filter logs based on time.
		/// </summary>
		public DateTime TimeStampBegin { get; set; } = DateTime.Now;

		/// <summary>
		/// The timestamp when the log ended in the local timezone. This is used to filter logs based on time.
		/// </summary>
		public DateTime TimeStampEnd { get; set; } = DateTime.Now;

		public double TimeLineOrdinalBegin { get; set; } = 0;

		public string TimeLineDetailsBegin { get; set; } = string.Empty;

		public double TimeLineOrdinalEnd { get; set; } = 0;

		public string TimeLineDetailsEnd { get; set; } = string.Empty;


		/// <summary>
		/// The ID of the chat where the log was created.
		/// </summary>
		public int SourceChatId { get; set; }

		/// <summary>
		/// The ID of the message that this log is associated with.
		/// </summary>
		public int SourceMessageId { get; set; }



		/// <summary>
		/// Gets the text of the log.
		/// </summary>
		public required string Text { get; init; }

		/// <summary>
		/// The status of the log.
		/// </summary>
		public MemoryLogStatus Status { get; set; } = MemoryLogStatus.Active;

		/// <summary>
		/// The ID of the log that this log consolidates into (if any).
		/// </summary>
		public int ConsolidatedBy { get; set; }

		/// <summary>
		/// The importance score of the log, which is a value between 0 and 1.0.
		/// </summary>
		public double Importance { get; set; } = 1.0;

		/// <summary>
		/// The number of tokens in the text of the log for the BM25 search.
		/// </summary>
		public int TokenCount { get; set; } = 0;
	}
}
