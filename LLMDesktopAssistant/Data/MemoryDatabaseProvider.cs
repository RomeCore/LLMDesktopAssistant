using System.Collections.Concurrent;
using LLMDesktopAssistant.Agents.Memory;
using LLMDesktopAssistant.Providers;
using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Data
{
	/// <summary>
	/// Default implementation of <see cref="IMemoryDatabaseProvider"/> backed by an
	/// <see cref="Utils.AsyncCache{TKey, TValue}"/> keyed by the memory block and the
	/// embedding model. Changing the embedding model transparently creates a fresh
	/// database instance, and the semantic sector is rebuilt with the new model.
	/// </summary>
	[Service(typeof(IMemoryDatabaseProvider))]
	public class MemoryDatabaseProvider : IMemoryDatabaseProvider, IDisposable
	{
		private readonly IModelManager _modelManager;
		private readonly Utils.AsyncCache<(Guid DataId, string ModelName), MemoryDatabase> _cache;
		private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = [];

		/// <summary>
		/// Initializes a new instance of the <see cref="MemoryDatabaseProvider"/> class.
		/// </summary>
		/// <param name="modelManager">The model manager used to resolve embedding models.</param>
		public MemoryDatabaseProvider(IModelManager modelManager)
		{
			_modelManager = modelManager;
			_cache = new Utils.AsyncCache<(Guid DataId, string ModelName), MemoryDatabase>(CreateAsync,
				slidingExpirationTime: TimeSpan.FromMinutes(10), cleanupInterval: TimeSpan.FromMinutes(10));
		}

		/// <inheritdoc/>
		public async Task<T> ExecuteAsync<T>(
			MemoryBlock block,
			Func<MemoryDatabase, CancellationToken, Task<T>> operation,
			CancellationToken cancellationToken = default)
		{
			var modelName = _modelManager.GetModel(block.EmbeddingModel).Descriptor.FullName;
			var key = (block.DataId, modelName);

			var semaphore = _locks.GetOrAdd(block.DataId, _ => new SemaphoreSlim(1));
			await semaphore.WaitAsync(cancellationToken);

			try
			{
				var db = await _cache.GetAsync(key, cancellationToken);
				return await operation(db, cancellationToken);
			}
			finally
			{
				semaphore.Release();
			}
		}

		/// <inheritdoc/>
		public async Task InvalidateAsync(Guid dataId)
		{
			foreach (var key in _cache.Keys)
			{
				if (key.DataId == dataId)
					await _cache.TryRemoveAsync(key);
			}
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			foreach (var key in _cache.Keys)
				_cache.TryRemove(key);

			_cache.Dispose();
		}

		private async Task<MemoryDatabase> CreateAsync((Guid DataId, string ModelName) key, CancellationToken cancellationToken)
		{
			var db = new MemoryDatabase(key.DataId.ToString(), _modelManager.GetModel(key.ModelName));

			if (!db.FactSector.IsModelActual)
				await db.FactSector.RebuildWithModelAsync();

			return db;
		}
	}
}
