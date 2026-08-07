using LLMDesktopAssistant.Data.MemoryModels;

namespace LLMDesktopAssistant.Agents.Memory
{
	/// <summary>
	/// Provides operations for storing and retrieving facts in agent memory blocks.
	/// </summary>
	public interface IMemoryFactStore
	{
		/// <summary>
		/// Stores a new fact into the specified memory block.
		/// </summary>
		/// <param name="block">The memory block to store the fact into.</param>
		/// <param name="fact">The text of the fact to store. Must not be empty or whitespace.</param>
		/// <param name="sourceChatId">The ID of the chat where the fact was created.</param>
		/// <param name="sourceMessageId">The ID of the message that the fact was created from.</param>
		/// <param name="importance">The importance score of the fact, between 0 and 1.0.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation. The result contains the stored fact.</returns>
		Task<MemoryFactResult> StoreAsync(
			MemoryBlock block,
			string fact,
			int sourceChatId = 0,
			int sourceMessageId = 0,
			double importance = 1.0,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Supersedes the specified active fact with a replacement fact. The old fact is
		/// marked as <see cref="MemoryFactStatus.Superseded"/>, removed from the keyword
		/// index and the semantic sector, and the replacement is stored as a new active fact.
		/// </summary>
		/// <param name="block">The memory block that contains the fact to supersede.</param>
		/// <param name="factId">The ID of the fact to supersede.</param>
		/// <param name="replacementText">The text of the replacement fact. Must not be empty or whitespace.</param>
		/// <param name="sourceChatId">The ID of the chat where the replacement fact was created.</param>
		/// <param name="sourceMessageId">The ID of the message that the replacement fact was created from.</param>
		/// <param name="importance">The importance score of the replacement fact, between 0 and 1.0.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation. The result contains the stored replacement fact.</returns>
		Task<MemoryFactResult> SupersedeAsync(
			MemoryBlock block,
			int factId,
			string replacementText,
			int sourceChatId = 0,
			int sourceMessageId = 0,
			double importance = 1.0,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Soft-deletes the specified fact by marking it as <see cref="MemoryFactStatus.Deleted"/>
		/// and removing it from the keyword index and the semantic sector. The fact record
		/// itself remains in the database so it can be restored.
		/// </summary>
		/// <param name="block">The memory block that contains the fact to delete.</param>
		/// <param name="factId">The ID of the fact to delete.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		Task SoftDeleteAsync(MemoryBlock block, int factId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Hard-deletes the specified fact by removing it from the database, the keyword
		/// index and the semantic sector.
		/// </summary>
		/// <param name="block">The memory block that contains the fact to delete.</param>
		/// <param name="factId">The ID of the fact to delete.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		Task HardDeleteAsync(MemoryBlock block, int factId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Restores a previously deleted or superseded fact back to
		/// <see cref="MemoryFactStatus.Active"/>, re-adding it to the keyword index and
		/// the semantic sector so that it becomes searchable again.
		/// </summary>
		/// <param name="block">The memory block that contains the fact to restore.</param>
		/// <param name="factId">The ID of the fact to restore.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		Task RestoreAsync(MemoryBlock block, int factId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Searches the specified memory block for facts relevant to the query.
		/// Combines vector similarity search with BM25 keyword search and merges
		/// the rankings of both retrievers using reciprocal rank fusion.
		/// </summary>
		/// <param name="block">The memory block to search.</param>
		/// <param name="query">The search query text. Must not be empty or whitespace.</param>
		/// <param name="maxCount">The maximum number of facts to return.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation. The result contains the matching facts.</returns>
		Task<MemoryFactResult[]> SearchAsync(
			MemoryBlock block,
			string query,
			int maxCount = 5,
			CancellationToken cancellationToken = default);
	}
}
