namespace LLMDesktopAssistant.LLM.Services.Storage
{
	/// <summary>
	/// Serializes composite storage operations of a chat and provides database transactions
	/// that are guaranteed to be rolled back on any failure. Every multi-row tree operation
	/// must go through this service so that an exception can never leave an open transaction
	/// behind (and, in turn, poison the storage of the current thread).
	/// </summary>
	public interface IChatStorageLock
	{
		/// <summary>
		/// Acquires the storage lock. The lock is reentrant, so the same thread may acquire it
		/// multiple times (including inside <see cref="DoTransaction"/>). Prefer
		/// <see cref="DoTransaction"/> when a whole sequence can be expressed as a single action;
		/// use Lock/Unlock to keep read-modify sequences atomic when the transaction
		/// has to be opened separately.
		/// </summary>
		void Lock();

		/// <summary>
		/// Releases the storage lock acquired via <see cref="Lock"/>.
		/// </summary>
		void Unlock();

		/// <summary>
		/// Executes <paramref name="action"/> inside a database transaction, serialized by this lock.
		/// The transaction is rolled back when the action throws, so a failure can never leave an
		/// open transaction behind.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// Thrown when the transaction cannot be begun or committed.
		/// </exception>
		void DoTransaction(Action action);
	}
}
