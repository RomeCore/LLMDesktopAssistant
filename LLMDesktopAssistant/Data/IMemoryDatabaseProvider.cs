using LLMDesktopAssistant.Agents.Memory;

namespace LLMDesktopAssistant.Data
{
	/// <summary>
	/// Provides centralized access to memory databases, caching them per memory block
	/// and embedding model. The provider also serializes operations on the same block.
	/// </summary>
	public interface IMemoryDatabaseProvider
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
		Task InvalidateAsync(Guid dataId);
	}
}
