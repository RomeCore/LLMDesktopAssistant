using LLMDesktopAssistant.Agents.Memory;

namespace LLMDesktopAssistant.Data
{
	/// <summary>
	/// Provides centralized access to memory databases, caching them per memory block
	/// and embedding model. The provider also serializes operations on the same block.
	/// </summary>
	public interface IMemoryDatabaseManager
	{
		/// <summary>
		/// Executes an operation against the memory database of the specified block,
		/// opening and caching the database as needed.
		/// </summary>
		/// <typeparam name="T">The type of the operation result.</typeparam>
		/// <param name="block">The memory block whose database is accessed.</param>
		/// <param name="operation">The operation to execute against the database.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation and contains the operation result.</returns>
		Task<T> ExecuteAsync<T>(
			MemoryBlock block,
			Func<MemoryDatabase, CancellationToken, Task<T>> operation,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Removes all cached databases of the specified memory block and disposes them.
		/// </summary>
		/// <param name="dataId">The identifier of the memory block to invalidate.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		Task InvalidateAsync(string dataId);

		/// <summary>
		/// Renames the memory database of the specified block, moving all associated files
		/// to the new identifier. Any cached databases are disposed beforehand, since
		/// <see cref="MemoryDatabase"/> keeps the underlying LiteDB file open.
		/// </summary>
		/// <param name="oldId">The current identifier of the memory block.</param>
		/// <param name="newId">The new identifier of the memory block.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		Task RenameAsync(string oldId, string newId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Copies the memory database of the source block to the new block identifier.
		/// </summary>
		/// <param name="sourceId">The identifier of the source memory block.</param>
		/// <param name="newId">The identifier of the new memory block.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		Task CopyAsync(string sourceId, string newId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Deletes the memory database files of the specified block.
		/// </summary>
		/// <param name="id">The identifier of the memory block to delete.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		Task DeleteAsync(string id, CancellationToken cancellationToken = default);
	}
}
