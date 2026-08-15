using LLMDesktopAssistant.Data.MemoryModels;

namespace LLMDesktopAssistant.Agents.Memory
{
	/// <summary>
	/// Provides operations for appending and retrieving episodic memory logs in memory blocks.
	/// Logs are immutable after creation: only their status can change.
	/// </summary>
	public interface IMemoryLogStore
	{
		/// <summary>
		/// Appends a new log entry to the specified memory block.
		/// </summary>
		/// <param name="block">The memory block to append the log to.</param>
		/// <param name="text">The text of the log. Must not be empty or whitespace.</param>
		/// <param name="initialStatus">The initial status of the log. Transient logs are excluded from text search.</param>
		/// <param name="timeStampBegin">The real-time timestamp when the log began. Defaults to now.</param>
		/// <param name="timeStampEnd">The real-time timestamp when the log ended. Defaults to <paramref name="timeStampBegin"/>.</param>
		/// <param name="timeLineOrdinalBegin">The game-time ordinal when the log began (for example, the day number).</param>
		/// <param name="timeLineDetailsBegin">The game-time details when the log began (for example, "Day 3, 14:00").</param>
		/// <param name="timeLineOrdinalEnd">The game-time ordinal when the log ended.</param>
		/// <param name="timeLineDetailsEnd">The game-time details when the log ended.</param>
		/// <param name="sourceChatId">The ID of the chat where the log was created.</param>
		/// <param name="sourceMessageId">The ID of the message that the log is associated with.</param>
		/// <param name="importance">The importance score of the log, between 0 and 1.0.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation. The result contains the appended log.</returns>
		Task<MemoryLogResult> AppendAsync(
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
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Searches the active logs of the specified memory block using BM25 keyword search.
		/// Transient, consolidated, ignored and deleted logs are excluded.
		/// </summary>
		/// <param name="block">The memory block to search.</param>
		/// <param name="query">The search query text. Must not be empty or whitespace.</param>
		/// <param name="minImportance">The minimum importance score of the logs. Logs with lower scores are excluded.</param>
		/// <param name="maxCount">The maximum number of logs to return.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation. The result contains the matching logs.</returns>
		Task<MemoryLogResult[]> SearchAsync(
			MemoryBlock block,
			string query,
			double minImportance = 0.0,
			int maxCount = 5,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets the active and transient logs of the specified memory block within the given
		/// time window (real time and/or alternative timeline). The logs are returned in descending
		/// order of their begin timestamp.
		/// </summary>
		/// <param name="block">The memory block to search.</param>
		/// <param name="from">The inclusive lower bound of the real-time window.</param>
		/// <param name="to">The inclusive upper bound of the real-time window.</param>
		/// <param name="timeLineFrom">The inclusive lower bound of the alternative time ordinal window.</param>
		/// <param name="timeLineTo">The inclusive upper bound of the alternative time ordinal window.</param>
		/// <param name="minImportance">The minimum importance score of the logs. Logs with lower scores are excluded.</param>
		/// <param name="maxCount">The maximum number of logs to return.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation. The result contains the matching logs.</returns>
		Task<MemoryLogResult[]> GetByTimeAsync(
			MemoryBlock block,
			DateTime? from = null,
			DateTime? to = null,
			double? timeLineFrom = null,
			double? timeLineTo = null,
			double minImportance = 0.0,
			int maxCount = 100,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets the active logs of the specified memory block for consolidation,
		/// in chronological order.
		/// </summary>
		/// <param name="block">The memory block to read from.</param>
		/// <param name="maxCount">The maximum number of logs to return.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation. The result contains the pending logs.</returns>
		Task<MemoryLogResult[]> GetPendingAsync(MemoryBlock block, int maxCount = 100, CancellationToken cancellationToken = default);

		/// <summary>
		/// Marks the specified log as <see cref="MemoryLogStatus.Transient"/> and removes it
		/// from text search. The log remains available through time-based search.
		/// </summary>
		/// <param name="block">The memory block that contains the log.</param>
		/// <param name="logId">The ID of the log to mark.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		Task MarkTransientAsync(MemoryBlock block, int logId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Marks the specified log as <see cref="MemoryLogStatus.Consolidated"/> and removes it
		/// from text search.
		/// </summary>
		/// <param name="block">The memory block that contains the log.</param>
		/// <param name="logId">The ID of the log to mark.</param>
		/// <param name="consolidatedIntoId">The ID of the log or fact that this log was consolidated into, or 0.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		Task MarkConsolidatedAsync(MemoryBlock block, int logId, int consolidatedIntoId = 0, CancellationToken cancellationToken = default);

		/// <summary>
		/// Marks the specified log as <see cref="MemoryLogStatus.Ignored"/> and removes it from
		/// text search. Used to record logs that were evaluated during consolidation and
		/// explicitly rejected, so they are not considered again.
		/// </summary>
		/// <param name="block">The memory block that contains the log.</param>
		/// <param name="logId">The ID of the log to mark.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		Task MarkIgnoredAsync(MemoryBlock block, int logId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Restores a previously marked log back to <see cref="MemoryLogStatus.Active"/>, re-adding
		/// it to text search.
		/// </summary>
		/// <param name="block">The memory block that contains the log.</param>
		/// <param name="logId">The ID of the log to restore.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		Task RestoreAsync(MemoryBlock block, int logId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Hard-deletes the specified log by removing it from the database and the keyword index.
		/// The log cannot be restored.
		/// </summary>
		/// <param name="block">The memory block that contains the log.</param>
		/// <param name="logId">The ID of the log to delete.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		Task HardDeleteAsync(MemoryBlock block, int logId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Clears all logs from the specified memory block, removing them from the
		/// database and the keyword index. The block configuration itself is preserved.
		/// </summary>
		/// <param name="block">The memory block to clear.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation. The result contains the number of removed logs.</returns>
		Task<int> ClearAsync(MemoryBlock block, CancellationToken cancellationToken = default);
	}
}
