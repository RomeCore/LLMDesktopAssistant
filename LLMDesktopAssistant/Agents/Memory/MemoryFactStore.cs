using LLMDesktopAssistant.Data;
using LLMDesktopAssistant.Data.MemoryModels;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Agents.Memory
{
	/// <summary>
	/// Default implementation of <see cref="IMemoryFactStore"/> backed by a hybrid storage:
	/// LiteDB keeps the facts and their BM25 keyword index, while a semantic sector
	/// provides vector search over the fact identifiers.
	/// </summary>
	[Service(typeof(IMemoryFactStore))]
	public class MemoryFactStore : IMemoryFactStore
	{
		private const int CandidateCount = 30;
		private const int RrfK = 60;
		private const double Bm25K1 = 1.2;
		private const double Bm25B = 0.75;

		private readonly IMemoryDatabaseManager _provider;

		/// <summary>
		/// Initializes a new instance of the <see cref="MemoryFactStore"/> class.
		/// </summary>
		/// <param name="provider">The provider used to access memory databases.</param>
		public MemoryFactStore(IMemoryDatabaseManager provider)
		{
			_provider = provider;
		}

		/// <inheritdoc/>
		public Task<MemoryFactResult> StoreAsync(
			MemoryBlock block,
			string fact,
			int sourceChatId = 0,
			int sourceMessageId = 0,
			double importance = 1.0,
			CancellationToken cancellationToken = default)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(fact);

			return _provider.ExecuteAsync(block, (db, ct) => StoreInternalAsync(db, fact, sourceChatId, sourceMessageId, importance, ct), cancellationToken);
		}

		/// <inheritdoc/>
		public Task<MemoryFactResult> SupersedeAsync(
			MemoryBlock block,
			int factId,
			string replacementText,
			int sourceChatId = 0,
			int sourceMessageId = 0,
			double importance = 1.0,
			CancellationToken cancellationToken = default)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(replacementText);

			return _provider.ExecuteAsync(block, async (db, ct) =>
			{
				var fact = db.Facts.FindById(factId)
					?? throw new KeyNotFoundException($"Fact with id {factId} not found in memory block '{block.Name}'.");

				var storedFact = await StoreInternalAsync(db, replacementText, sourceChatId, sourceMessageId, importance, ct);

				if (fact.Status == MemoryFactStatus.Active)
				{
					fact.Status = MemoryFactStatus.Superseded;
					fact.SupersededBy = storedFact.Id;
					fact.UpdatedAt = DateTime.Now;
					db.Facts.Update(fact);
				}

				db.FactIndexes.DeleteMany(fi => fi.FactId == factId);
				db.FactSector.DeleteWhere(id => id == factId);

				return storedFact;
			}, cancellationToken);
		}

		/// <inheritdoc/>
		public async Task SoftDeleteAsync(MemoryBlock block, int factId, CancellationToken cancellationToken = default)
		{
			await _provider.ExecuteAsync(block, (db, _) =>
			{
				var fact = db.Facts.FindById(factId)
					?? throw new KeyNotFoundException($"Fact with id {factId} not found in memory block '{block.Name}'.");

				if (fact.Status == MemoryFactStatus.Active)
				{
					fact.Status = MemoryFactStatus.Deleted;
					fact.UpdatedAt = DateTime.Now;
					db.Facts.Update(fact);
				}

				db.FactIndexes.DeleteMany(fi => fi.FactId == factId);
				db.FactSector.DeleteWhere(id => id == factId);

				return Task.FromResult(true);
			}, cancellationToken);
		}

		/// <inheritdoc/>
		public async Task HardDeleteAsync(MemoryBlock block, int factId, CancellationToken cancellationToken = default)
		{
			await _provider.ExecuteAsync(block, (db, _) =>
			{
				if (db.Facts.FindById(factId) is null)
					throw new KeyNotFoundException($"Fact with id {factId} not found in memory block '{block.Name}'.");

				db.Facts.Delete(factId);
				db.FactIndexes.DeleteMany(fi => fi.FactId == factId);
				db.FactSector.DeleteWhere(id => id == factId);
				return Task.FromResult(true);
			}, cancellationToken);
		}

		/// <inheritdoc/>
		public async Task<int> ClearAsync(MemoryBlock block, CancellationToken cancellationToken = default)
		{
			var (facts, _) = await _provider.ClearAsync(block, clearFacts: true, clearLogs: false, cancellationToken);
			return facts;
		}

		/// <inheritdoc/>
		public async Task RestoreAsync(MemoryBlock block, int factId, CancellationToken cancellationToken = default)
		{
			await _provider.ExecuteAsync(block, async (db, ct) =>
			{
				var fact = db.Facts.FindById(factId)
					?? throw new KeyNotFoundException($"Fact with id {factId} not found in memory block '{block.Name}'.");

				if (fact.Status == MemoryFactStatus.Active)
					return true;

				var previousStatus = fact.Status;
				var previousSupersededBy = fact.SupersededBy;

				fact.Status = MemoryFactStatus.Active;
				fact.SupersededBy = 0;
				fact.UpdatedAt = DateTime.Now;
				db.Facts.Update(fact);

				try
				{
					foreach (var (token, count) in MemoryTokenizer.Tokenize(fact.Text).GroupBy(t => t).Select(g => (g.Key, g.Count())))
					{
						db.FactIndexes.Insert(new MemoryFactIndexModel
						{
							FactId = fact.Id,
							Token = token,
							Count = count
						});
					}

					await db.FactSector.RecordAsync(fact.Id, ct);
				}
				catch
				{
					fact.Status = previousStatus;
					fact.SupersededBy = previousSupersededBy;
					fact.UpdatedAt = DateTime.Now;
					db.Facts.Update(fact);
					db.FactIndexes.DeleteMany(fi => fi.FactId == fact.Id);
					throw;
				}

				return true;
			}, cancellationToken);
		}

		private static async Task<MemoryFactResult> StoreInternalAsync(
			MemoryDatabase db,
			string fact,
			int sourceChatId,
			int sourceMessageId,
			double importance,
			CancellationToken cancellationToken)
		{
			var tokens = MemoryTokenizer.Tokenize(fact);

			var model = new MemoryFactModel
			{
				Text = fact,
				SourceChatId = sourceChatId,
				SourceMessageId = sourceMessageId,
				Status = MemoryFactStatus.Active,
				Importance = importance,
				TokenCount = tokens.Count
			};

			db.Facts.Insert(model);

			foreach (var (token, count) in tokens.GroupBy(t => t).Select(g => (g.Key, g.Count())))
			{
				db.FactIndexes.Insert(new MemoryFactIndexModel
				{
					FactId = model.Id,
					Token = token,
					Count = count
				});
			}

			try
			{
				await db.FactSector.RecordAsync(model.Id, cancellationToken);
			}
			catch
			{
				db.Facts.Delete(model.Id);
				db.FactIndexes.DeleteMany(fi => fi.FactId == model.Id);
				throw;
			}

			return new MemoryFactResult
			{
				Id = model.Id,
				Text = model.Text,
				Status = MemoryFactStatus.Active,
				CreatedAt = model.CreatedAt,
				UpdatedAt = model.UpdatedAt,
				LastAccessedAt = model.LastAccessedAt,
				AccessCount = model.AccessCount,
				Importance = model.Importance,
				CosineScore = null,
				Bm25Score = null,
				RrfScore = null
			};
		}

		/// <inheritdoc/>
		public Task<MemoryFactResult[]> SearchAsync(
			MemoryBlock block,
			string query,
			int maxCount = 5,
			CancellationToken cancellationToken = default)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(query);
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCount);

			return _provider.ExecuteAsync(block, (db, ct) => SearchInternalAsync(db, query, maxCount, ct), cancellationToken);
		}

		private static async Task<MemoryFactResult[]> SearchInternalAsync(
			MemoryDatabase db,
			string query,
			int maxCount,
			CancellationToken cancellationToken)
		{
			if (db.FactSector.Count == 0)
				return [];

			var vectorResults = await db.FactSector.QueryAsync(query, CandidateCount, cancellationToken: cancellationToken);
			var bm25Results = Bm25Search(db, query, CandidateCount);

			var vectorScores = vectorResults.ToDictionary(r => r.Item, r => (double)r.Score);
			var bm25Scores = bm25Results.ToDictionary(r => r.Id, r => r.Score);

			var fused = RrfFuse(vectorResults.Select(r => r.Item).ToArray(), bm25Results.Select(r => r.Id).ToArray(), RrfK);

			var facts = new List<MemoryFactResult>();
			foreach (var (id, rrfScore) in fused)
			{
				if (facts.Count >= maxCount)
					break;

				var fact = db.Facts.FindById(id);
				if (fact is { Status: MemoryFactStatus.Active })
				{
					fact.AccessCount++;
					fact.LastAccessedAt = DateTime.Now;
					db.Facts.Update(fact);

					facts.Add(new MemoryFactResult
					{
						Id = fact.Id,
						Text = fact.Text,
						Status = fact.Status,
						CreatedAt = fact.CreatedAt,
						UpdatedAt = fact.UpdatedAt,
						LastAccessedAt = fact.LastAccessedAt,
						AccessCount = fact.AccessCount,
						Importance = fact.Importance,
						CosineScore = vectorScores.TryGetValue(fact.Id, out double cosine) ? cosine : null,
						Bm25Score = bm25Scores.TryGetValue(fact.Id, out double bm25) ? bm25 : null,
						RrfScore = rrfScore
					});
				}
			}

			return facts.ToArray();
		}

		/// <summary>
		/// Scores the active facts with the BM25 ranking function and returns
		/// the best-scoring facts in descending order. The BM25 ranking is fused
		/// with the vector ranking using reciprocal rank fusion.
		/// </summary>
		/// <param name="db">The memory database to search.</param>
		/// <param name="query">The search query text.</param>
		/// <param name="topN">The maximum number of facts to return.</param>
		/// <returns>The best-scoring facts together with their BM25 scores.</returns>
		private static (int Id, double Score)[] Bm25Search(MemoryDatabase db, string query, int topN)
		{
			var tokens = MemoryTokenizer.Tokenize(query);
			if (tokens.Count == 0)
				return [];

			var activeFacts = db.Facts.Find(f => f.Status == MemoryFactStatus.Active).ToList();
			if (activeFacts.Count == 0)
				return [];

			var documentLengths = activeFacts.ToDictionary(f => f.Id, f => Math.Max(f.TokenCount, 1));
			int documentCount = documentLengths.Count;
			double averageDocumentLength = documentLengths.Values.Average();

			var scores = new Dictionary<int, double>();

			foreach (var token in tokens.Distinct())
			{
				var postings = db.FactIndexes.Find(i => i.Token == token).ToList();

				int documentFrequency = postings.Count(p => documentLengths.ContainsKey(p.FactId));
				if (documentFrequency == 0)
					continue;

				double idf = Math.Log(1 + (documentCount - documentFrequency + 0.5) / (documentFrequency + 0.5));

				foreach (var posting in postings)
				{
					if (!documentLengths.TryGetValue(posting.FactId, out int documentLength))
						continue;

					double termFrequency = posting.Count;
					double normalizedTermFrequency = termFrequency * (Bm25K1 + 1) /
						(termFrequency + Bm25K1 * (1 - Bm25B + Bm25B * documentLength / averageDocumentLength));

					scores[posting.FactId] = scores.GetValueOrDefault(posting.FactId) + idf * normalizedTermFrequency;
				}
			}

			return scores
				.OrderByDescending(pair => pair.Value)
				.Take(topN)
				.Select(pair => (pair.Key, pair.Value))
				.ToArray();
		}

		/// <summary>
		/// Merges two ranked fact identifier lists using reciprocal rank fusion.
		/// The fused score is relative and only meaningful for comparing the items
		/// within a single result set.
		/// </summary>
		/// <param name="first">The first ranked list.</param>
		/// <param name="second">The second ranked list.</param>
		/// <param name="k">The RRF constant that dampens the contribution of the top ranks.</param>
		/// <returns>The fused fact identifiers together with their fused scores, in descending order of the score.</returns>
		private static (int Id, double Score)[] RrfFuse(IReadOnlyList<int> first, IReadOnlyList<int> second, int k)
		{
			var scores = new Dictionary<int, double>();

			for (int i = 0; i < first.Count; i++)
				scores[first[i]] = scores.GetValueOrDefault(first[i]) + 1.0 / (k + i + 1);
			for (int i = 0; i < second.Count; i++)
				scores[second[i]] = scores.GetValueOrDefault(second[i]) + 1.0 / (k + i + 1);

			return scores
				.OrderByDescending(pair => pair.Value)
				.Select(pair => (pair.Key, pair.Value))
				.ToArray();
		}
	}
}
