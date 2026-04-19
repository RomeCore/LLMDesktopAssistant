using System;
using LiteDB;

namespace LLMDesktopAssistant.Data.UsageModels
{
	/// <summary>
	/// Represents a single usage record in the database.
	/// </summary>
	public class UsageRecordModel
	{
		/// <summary>
		/// The unique identifier for the usage record.
		/// </summary>
		[BsonId]
		public int Id { get; set; }

		/// <summary>
		/// The model used for the request (e.g., "gpt-4", "claude-3").
		/// </summary>
		public string Model { get; set; } = string.Empty;

		/// <summary>
		/// Number of input tokens used in the request.
		/// </summary>
		public int InputTokens { get; set; }

		/// <summary>
		/// Number of output tokens generated in the response.
		/// </summary>
		public int OutputTokens { get; set; }

		/// <summary>
		/// Number of tokens served from cache.
		/// </summary>
		public int CacheHitTokens { get; set; }

		/// <summary>
		/// Number of tokens not found in cache.
		/// </summary>
		public int CacheMissTokens { get; set; }

		/// <summary>
		/// Duration of the request in milliseconds.
		/// </summary>
		public long DurationMs { get; set; }

		/// <summary>
		/// Whether the request was successful.
		/// </summary>
		public bool Success { get; set; }

		/// <summary>
		/// Error message if the request failed.
		/// </summary>
		public string? ErrorMessage { get; set; }

		/// <summary>
		/// The timestamp when the usage record was created.
		/// </summary>
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;
	}
}