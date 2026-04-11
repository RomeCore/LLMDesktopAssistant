using System;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Core.LLM.Services
{
	/// <summary>
	/// Interface for collecting usage statistics and storing them in a separate database.
	/// </summary>
	public interface IUsageStatsCollector
	{
		/// <summary>
		/// Records a usage event with the specified parameters.
		/// </summary>
		/// <param name="model">The model used (e.g., "gpt-4", "claude-3").</param>
		/// <param name="inputTokens">Number of input tokens used.</param>
		/// <param name="outputTokens">Number of output tokens generated.</param>
		/// <param name="cacheHitTokens">Number of tokens served from cache (optional).</param>
		/// <param name="cacheMissTokens">Number of tokens not found in cache (optional).</param>
		/// <param name="durationMs">Duration of the request in milliseconds (optional).</param>
		/// <param name="success">Whether the request was successful.</param>
		/// <param name="errorMessage">Error message if the request failed (optional).</param>
		void RecordUsage(
			string model,
			int inputTokens,
			int outputTokens,
			int cacheHitTokens = 0,
			int cacheMissTokens = 0,
			long durationMs = 0,
			bool success = true,
			string? errorMessage = null);

		/// <summary>
		/// Gets aggregated usage statistics for a specific time period.
		/// </summary>
		/// <param name="startDate">Start date for the statistics (optional).</param>
		/// <param name="endDate">End date for the statistics (optional).</param>
		/// <returns>Aggregated usage statistics.</returns>
		UsageStatistics GetStatistics(DateTime? startDate = null, DateTime? endDate = null);

		/// <summary>
		/// Clears all usage statistics from the database.
		/// </summary>
		void ClearStatistics();
	}

	/// <summary>
	/// Represents aggregated usage statistics.
	/// </summary>
	public class UsageStatistics
	{
		public int TotalRequests { get; set; }
		public int SuccessfulRequests { get; set; }
		public int FailedRequests { get; set; }
		public int TotalInputTokens { get; set; }
		public int TotalOutputTokens { get; set; }
		public int TotalCacheHitTokens { get; set; }
		public int TotalCacheMissTokens { get; set; }
		public double AverageDurationMs { get; set; }
		public DateTime? FirstRequestTime { get; set; }
		public DateTime? LastRequestTime { get; set; }
		public Dictionary<string, ModelUsageStatistics> ModelStatistics { get; set; } = new Dictionary<string, ModelUsageStatistics>();
	}

	/// <summary>
	/// Represents usage statistics for a specific model.
	/// </summary>
	public class ModelUsageStatistics
	{
		public int TotalRequests { get; set; }
		public int SuccessfulRequests { get; set; }
		public int FailedRequests { get; set; }
		public int TotalInputTokens { get; set; }
		public int TotalOutputTokens { get; set; }
		public int TotalCacheHitTokens { get; set; }
		public int TotalCacheMissTokens { get; set; }
		public double AverageDurationMs { get; set; }
	}
}