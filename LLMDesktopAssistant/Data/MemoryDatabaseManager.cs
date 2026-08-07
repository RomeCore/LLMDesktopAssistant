using System.Collections.Concurrent;
using LLMDesktopAssistant.Agents.Memory;
using LLMDesktopAssistant.Providers;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Data
{
	/// <summary>
	/// Default implementation of <see cref="IMemoryDatabaseManager"/> backed by an
	/// <see cref="Utils.AsyncCache{TKey, TValue}"/> keyed by the memory block and the
	/// embedding model. Changing the embedding model transparently creates a fresh
	/// database instance, and the semantic sector is rebuilt with the new model.
	/// </summary>
	[Service(typeof(IMemoryDatabaseManager))]
	public class MemoryDatabaseManager : IMemoryDatabaseManager, IDisposable
	{
		private readonly IModelManager _modelManager;
		private readonly Utils.AsyncCache<(string DataId, string ModelName), MemoryDatabase> _cache;
		private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = [];

		/// <summary>
		/// Initializes a new instance of the <see cref="MemoryDatabaseManager"/> class.
		/// </summary>
		/// <param name="modelManager">The model manager used to resolve embedding models.</param>
		public MemoryDatabaseManager(IModelManager modelManager)
		{
			_modelManager = modelManager;
			_cache = new Utils.AsyncCache<(string DataId, string ModelName), MemoryDatabase>(CreateAsync,
				slidingExpirationTime: TimeSpan.FromMinutes(10), cleanupInterval: TimeSpan.FromMinutes(10));
		}

		/// <inheritdoc/>
		public async Task<T> ExecuteAsync<T>(
			MemoryBlock block,
			Func<MemoryDatabase, CancellationToken, Task<T>> operation,
			CancellationToken cancellationToken = default)
		{
			var modelName = _modelManager.GetModel(block.EmbeddingModel).Descriptor.FullName;
			var key = (block.Id, modelName);

			var semaphore = _locks.GetOrAdd(block.Id, _ => new SemaphoreSlim(1));
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
		public async Task InvalidateAsync(string dataId)
		{
			foreach (var key in _cache.Keys)
			{
				if (key.DataId == dataId)
					await _cache.TryRemoveAsync(key);
			}
		}

		/// <inheritdoc/>
		public async Task RenameAsync(string oldId, string newId, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(oldId) || string.IsNullOrEmpty(newId) || oldId == newId)
				return;

			var semaphore = _locks.GetOrAdd(oldId, _ => new SemaphoreSlim(1));
			await semaphore.WaitAsync(cancellationToken);
			try
			{
				// Dispose cached databases first: LiteDB keeps the file open and would block the move.
				await InvalidateAsync(oldId);
				await InvalidateAsync(newId);

				var sourceDir = GetBlockDirectory(oldId);
				if (Directory.Exists(sourceDir))
				{
					var targetDir = GetBlockDirectory(newId);
					if (Directory.Exists(targetDir))
						Directory.Delete(targetDir, recursive: true);
					Directory.Move(sourceDir, targetDir);
				}
			}
			finally
			{
				semaphore.Release();
			}

			_locks.TryRemove(oldId, out _);
		}

		/// <inheritdoc/>
		public async Task CopyAsync(string sourceId, string newId, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(sourceId) || string.IsNullOrEmpty(newId) || sourceId == newId)
				return;

			var semaphore = _locks.GetOrAdd(sourceId, _ => new SemaphoreSlim(1));
			await semaphore.WaitAsync(cancellationToken);
			try
			{
				// Dispose the cached databases so the copy is a consistent snapshot
				// and no open file handles block the file operations.
				await InvalidateAsync(sourceId);
				await InvalidateAsync(newId);

				var sourceDir = GetBlockDirectory(sourceId);
				if (!Directory.Exists(sourceDir))
					return;

				var targetDir = GetBlockDirectory(newId);
				if (Directory.Exists(targetDir))
					Directory.Delete(targetDir, recursive: true);

				CopyDirectory(sourceDir, targetDir);
			}
			finally
			{
				semaphore.Release();
			}
		}

		/// <inheritdoc/>
		public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(id))
				return;

			var semaphore = _locks.GetOrAdd(id, _ => new SemaphoreSlim(1));
			await semaphore.WaitAsync(cancellationToken);
			try
			{
				await InvalidateAsync(id);

				var dir = GetBlockDirectory(id);
				if (Directory.Exists(dir))
					Directory.Delete(dir, recursive: true);
			}
			finally
			{
				semaphore.Release();
			}

			_locks.TryRemove(id, out _);
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			foreach (var key in _cache.Keys)
				_cache.TryRemove(key);

			_cache.Dispose();
		}

		private async Task<MemoryDatabase> CreateAsync((string DataId, string ModelName) key, CancellationToken cancellationToken)
		{
			var db = new MemoryDatabase(key.DataId.ToString(), _modelManager.GetModel(key.ModelName));

			if (!db.FactSector.IsModelActual)
				await db.FactSector.RebuildWithModelAsync();

			return db;
		}

		private static string GetBlockDirectory(string id) => Path.Combine(Directories.Memory, id);

		private static void CopyDirectory(string sourceDir, string targetDir)
		{
			Directory.CreateDirectory(targetDir);
			foreach (var file in Directory.EnumerateFiles(sourceDir))
				File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), overwrite: true);
			foreach (var subDir in Directory.EnumerateDirectories(sourceDir))
				CopyDirectory(subDir, Path.Combine(targetDir, Path.GetFileName(subDir)));
		}
	}
}
