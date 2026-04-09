using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using LLMDesktopAssistant.LLM.Data;
using LLMDesktopAssistant.LLM.Data.Models;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// Implementation of the usage statistics collector that stores data in a separate LiteDB database.
	/// </summary>
	public class UsageStatsCollector : IUsageStatsCollector
	{
		private readonly UsageDatabase _database;

		public UsageStatsCollector(UsageDatabase database)
		{
			_database = database ?? throw new ArgumentNullException(nameof(database));
		}

		public void RecordUsage(
			string model,
			int inputTokens,
			int outputTokens,
			int cacheHitTokens = 0,
			int cacheMissTokens = 0,
			long durationMs = 0,
			bool success = true,
			string? errorMessage = null)
		{
			if (string.IsNullOrWhiteSpace(model))
				throw new ArgumentException("Model name cannot be null or whitespace.", nameof(model));

			var record = new UsageRecordModel
			{
				Model = model,
				InputTokens = inputTokens,
				OutputTokens = outputTokens,
				CacheHitTokens = cacheHitTokens,
				CacheMissTokens = cacheMissTokens,
				DurationMs = durationMs,
				Success = success,
				ErrorMessage = errorMessage,
				Timestamp = DateTime.UtcNow
			};

			_database.UsageRecords.Insert(record);
		}

		public UsageStatistics GetStatistics(DateTime? startDate = null, DateTime? endDate = null)
		{
			var query = _database.UsageRecords.FindAll().AsQueryable();

			if (startDate.HasValue)
				query = query.Where(r => r.Timestamp >= startDate.Value);

			if (endDate.HasValue)
				query = query.Where(r => r.Timestamp <= endDate.Value);

			var records = query.ToList();

			var statistics = new UsageStatistics
			{
				TotalRequests = records.Count,
				SuccessfulRequests = records.Count(r => r.Success),
				FailedRequests = records.Count(r => !r.Success),
				TotalInputTokens = records.Sum(r => r.InputTokens),
				TotalOutputTokens = records.Sum(r => r.OutputTokens),
				TotalCacheHitTokens = records.Sum(r => r.CacheHitTokens),
				TotalCacheMissTokens = records.Sum(r => r.CacheMissTokens),
				AverageDurationMs = records.Any() ? records.Average(r => r.DurationMs) : 0,
				FirstRequestTime = records.Any() ? records.Min(r => r.Timestamp) : null,
				LastRequestTime = records.Any() ? records.Max(r => r.Timestamp) : null
			};

			// Group by model and calculate statistics for each model
			var modelGroups = records.GroupBy(r => r.Model);
			foreach (var group in modelGroups)
			{
				var modelRecords = group.ToList();
				statistics.ModelStatistics[group.Key] = new ModelUsageStatistics
				{
					TotalRequests = modelRecords.Count,
					SuccessfulRequests = modelRecords.Count(r => r.Success),
					FailedRequests = modelRecords.Count(r => !r.Success),
					TotalInputTokens = modelRecords.Sum(r => r.InputTokens),
					TotalOutputTokens = modelRecords.Sum(r => r.OutputTokens),
					TotalCacheHitTokens = modelRecords.Sum(r => r.CacheHitTokens),
					TotalCacheMissTokens = modelRecords.Sum(r => r.CacheMissTokens),
					AverageDurationMs = modelRecords.Any() ? modelRecords.Average(r => r.DurationMs) : 0
				};
			}

			return statistics;
		}

		public void ClearStatistics()
		{
			_database.UsageRecords.DeleteAll();
		}
	}
}