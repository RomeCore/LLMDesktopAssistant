using LLMDesktopAssistant.Data;
using LLMDesktopAssistant.Data.MemoryModels;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Agents.Memory
{
	/// <summary>
	/// Default implementation of <see cref="IMemoryLogStore"/> backed by the LiteDB
	/// collections for log entries and their BM25 keyword index.
	/// </summary>
	[Service(typeof(IMemoryLogStore))]
	public class MemoryLogStore : IMemoryLogStore
	{
		private const double Bm25K1 = 1.2;
		private const double Bm25B = 0.75;

		private readonly IMemoryDatabaseManager _provider;

		/// <summary>
		/// Initializes a new instance of the <see cref="MemoryLogStore"/> class.
		/// </summary>
		/// <param name="provider">The provider used to access memory databases.</param>
		public MemoryLogStore(IMemoryDatabaseManager provider)
		{
			_provider = provider;
		}

		/// <inheritdoc/>
		public Task<MemoryLogResult> AppendAsync(
			MemoryBlock block,
			string text,
			MemoryLogStatus initialStatus = MemoryLogStatus.Active,
			DateTime? timeStampBegin = null,
			DateTime? timeStampEnd = null,
			double timeLineOrdinalBegin = 0,
			string timeLineDetailsBegin = "",
			double timeLineOrdinalEnd = 0,
			string timeLineDetailsEnd = "",
			int sourceChatId = 0,
			int sourceMessageId = 0,
			double importance = 1.0,
			CancellationToken cancellationToken = default)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(text);

			return _provider.ExecuteAsync(block, (db, _) =>
			{
				var tokens = MemoryTokenizer.Tokenize(text);
				var now = DateTime.UtcNow;

				var model = new MemoryLogModel
				{
					Text = text,
					Status = initialStatus,
					TimeStampBegin = timeStampBegin ?? now,
					TimeStampEnd = timeStampEnd ?? timeStampBegin ?? now,
					TimeLineOrdinalBegin = timeLineOrdinalBegin,
					TimeLineDetailsBegin = timeLineDetailsBegin,
					TimeLineOrdinalEnd = timeLineOrdinalEnd,
					TimeLineDetailsEnd = timeLineDetailsEnd,
					SourceChatId = sourceChatId,
					SourceMessageId = sourceMessageId,
					Importance = importance,
					TokenCount = tokens.Count
				};

				db.Logs.Insert(model);

				if (model.Status == MemoryLogStatus.Active)
				{
					foreach (var (token, count) in tokens.GroupBy(t => t).Select(g => (g.Key, g.Count())))
					{
						db.LogIndexes.Insert(new MemoryLogIndexModel
						{
							LogId = model.Id,
							Token = token,
							Count = count
						});
					}
				}

				return Task.FromResult(ToResult(model, null));
			}, cancellationToken);
		}

		/// <inheritdoc/>
		public Task<MemoryLogResult[]> SearchAsync(
			MemoryBlock block,
			string query,
			int maxCount = 5,
			CancellationToken cancellationToken = default)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(query);
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCount);

			return _provider.ExecuteAsync(block, (db, _) => Task.FromResult(SearchInternal(db, query, maxCount)), cancellationToken);
		}

		/// <inheritdoc/>
		public Task<MemoryLogResult[]> GetByTimeAsync(
			MemoryBlock block,
			DateTime? from = null,
			DateTime? to = null,
			double? timeLineFrom = null,
			double? timeLineTo = null,
			int maxCount = 100,
			CancellationToken cancellationToken = default)
		{
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCount);

			return _provider.ExecuteAsync(block, (db, _) => Task.FromResult(GetByTimeInternal(db, from, to, timeLineFrom, timeLineTo, maxCount)), cancellationToken);
		}

		/// <inheritdoc/>
		public Task<MemoryLogResult[]> GetPendingAsync(
			MemoryBlock block,
			int maxCount = 100,
			CancellationToken cancellationToken = default)
		{
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCount);

			return _provider.ExecuteAsync(block, (db, _) => Task.FromResult(GetPendingInternal(db, maxCount)), cancellationToken);
		}

		/// <inheritdoc/>
		public Task MarkTransientAsync(MemoryBlock block, int logId, CancellationToken cancellationToken = default)
			=> SetStatusAsync(block, logId, MemoryLogStatus.Transient, 0, cancellationToken);

		/// <inheritdoc/>
		public Task MarkConsolidatedAsync(MemoryBlock block, int logId, int consolidatedIntoId = 0, CancellationToken cancellationToken = default)
			=> SetStatusAsync(block, logId, MemoryLogStatus.Consolidated, consolidatedIntoId, cancellationToken);

		/// <inheritdoc/>
		public Task MarkIgnoredAsync(MemoryBlock block, int logId, CancellationToken cancellationToken = default)
			=> SetStatusAsync(block, logId, MemoryLogStatus.Ignored, 0, cancellationToken);

		/// <inheritdoc/>
		public async Task HardDeleteAsync(MemoryBlock block, int logId, CancellationToken cancellationToken = default)
		{
			await _provider.ExecuteAsync(block, (db, _) =>
			{
				if (db.Logs.FindById(logId) is null)
					throw new KeyNotFoundException($"Log with id {logId} not found in memory block '{block.Name}'.");

				db.Logs.Delete(logId);
				db.LogIndexes.DeleteMany(i => i.LogId == logId);
				return Task.FromResult(true);
			}, cancellationToken);
		}

		/// <inheritdoc/>
		public async Task RestoreAsync(MemoryBlock block, int logId, CancellationToken cancellationToken = default)
		{
			await _provider.ExecuteAsync(block, async (db, ct) =>
			{
				var log = db.Logs.FindById(logId)
					?? throw new KeyNotFoundException($"Log with id {logId} not found in memory block '{block.Name}'.");

				if (log.Status == MemoryLogStatus.Active)
					return true;

				var previousStatus = log.Status;
				var previousConsolidatedBy = log.ConsolidatedBy;

				log.Status = MemoryLogStatus.Active;
				log.ConsolidatedBy = 0;
				log.UpdatedAt = DateTime.UtcNow;
				db.Logs.Update(log);

				try
				{
					foreach (var (token, count) in MemoryTokenizer.Tokenize(log.Text).GroupBy(t => t).Select(g => (g.Key, g.Count())))
					{
						db.LogIndexes.Insert(new MemoryLogIndexModel
						{
							LogId = log.Id,
							Token = token,
							Count = count
						});
					}
				}
				catch
				{
					log.Status = previousStatus;
					log.ConsolidatedBy = previousConsolidatedBy;
					log.UpdatedAt = DateTime.UtcNow;
					db.Logs.Update(log);
					db.LogIndexes.DeleteMany(i => i.LogId == log.Id);
					throw;
				}

				return true;
			}, cancellationToken);
		}

		private Task SetStatusAsync(MemoryBlock block, int logId, MemoryLogStatus status, int consolidatedIntoId, CancellationToken cancellationToken)
		{
			return _provider.ExecuteAsync(block, (db, _) =>
			{
				var log = db.Logs.FindById(logId)
					?? throw new KeyNotFoundException($"Log with id {logId} not found in memory block '{block.Name}'.");

				log.Status = status;
				log.ConsolidatedBy = consolidatedIntoId;
				log.UpdatedAt = DateTime.UtcNow;
				db.Logs.Update(log);

				db.LogIndexes.DeleteMany(i => i.LogId == logId);

				return Task.FromResult(true);
			}, cancellationToken);
		}

		private static MemoryLogResult[] SearchInternal(MemoryDatabase db, string query, int maxCount)
		{
			var tokens = MemoryTokenizer.Tokenize(query);
			if (tokens.Count == 0)
				return [];

			var activeLogs = db.Logs.Find(l => l.Status == MemoryLogStatus.Active).ToList();
			if (activeLogs.Count == 0)
				return [];

			var documentLengths = activeLogs.ToDictionary(l => l.Id, l => Math.Max(l.TokenCount, 1));
			int documentCount = documentLengths.Count;
			double averageDocumentLength = documentLengths.Values.Average();

			var scores = new Dictionary<int, double>();

			foreach (var token in tokens.Distinct())
			{
				var postings = db.LogIndexes.Find(i => i.Token == token).ToList();

				int documentFrequency = postings.Count(p => documentLengths.ContainsKey(p.LogId));
				if (documentFrequency == 0)
					continue;

				double idf = Math.Log(1 + (documentCount - documentFrequency + 0.5) / (documentFrequency + 0.5));

				foreach (var posting in postings)
				{
					if (!documentLengths.TryGetValue(posting.LogId, out int documentLength))
						continue;

					double termFrequency = posting.Count;
					double normalizedTermFrequency = termFrequency * (Bm25K1 + 1) /
						(termFrequency + Bm25K1 * (1 - Bm25B + Bm25B * documentLength / averageDocumentLength));

					scores[posting.LogId] = scores.GetValueOrDefault(posting.LogId) + idf * normalizedTermFrequency;
				}
			}

			return scores
				.OrderByDescending(pair => pair.Value)
				.Take(maxCount)
				.Select(pair => ToResult(db.Logs.FindById(pair.Key), pair.Value))
				.ToArray();
		}

		private static MemoryLogResult[] GetByTimeInternal(
			MemoryDatabase db,
			DateTime? from,
			DateTime? to,
			double? timeLineFrom,
			double? timeLineTo,
			int maxCount)
		{
			return db.Logs
				.Find(l => l.Status == MemoryLogStatus.Active || l.Status == MemoryLogStatus.Transient)
				.Where(l =>
					(from == null || l.TimeStampEnd >= from.Value) &&
					(to == null || l.TimeStampBegin <= to.Value) &&
					(timeLineFrom == null || l.TimeLineOrdinalEnd >= timeLineFrom.Value) &&
					(timeLineTo == null || l.TimeLineOrdinalBegin <= timeLineTo.Value))
				.OrderByDescending(l => l.TimeStampBegin)
				.Take(maxCount)
				.Select(l => ToResult(l, null))
				.ToArray();
		}

		private static MemoryLogResult[] GetPendingInternal(MemoryDatabase db, int maxCount)
		{
			return db.Logs
				.Find(l => l.Status == MemoryLogStatus.Active)
				.OrderBy(l => l.TimeStampBegin)
				.Take(maxCount)
				.Select(l => ToResult(l, null))
				.ToArray();
		}

		private static MemoryLogResult ToResult(MemoryLogModel log, double? bm25Score)
		{
			return new MemoryLogResult
			{
				Id = log.Id,
				Text = log.Text,
				Status = log.Status,
				CreatedAt = log.CreatedAt,
				TimeStampBegin = log.TimeStampBegin,
				TimeStampEnd = log.TimeStampEnd,
				TimeLineOrdinalBegin = log.TimeLineOrdinalBegin,
				TimeLineDetailsBegin = log.TimeLineDetailsBegin,
				TimeLineOrdinalEnd = log.TimeLineOrdinalEnd,
				TimeLineDetailsEnd = log.TimeLineDetailsEnd,
				SourceChatId = log.SourceChatId,
				SourceMessageId = log.SourceMessageId,
				Importance = log.Importance,
				Bm25Score = bm25Score
			};
		}
	}
}
