using LLMDesktopAssistant.Data;

namespace LLMDesktopAssistant.LLM.Services.Storage
{
	/// <summary>
	/// Default <see cref="IChatStorageLock"/>: a reentrant lock paired with LiteDB transactions
	/// for the chat database of the current chat scope. LiteDB transactions are bound to their
	/// thread, so the lock serializes composite operations while regular auto-save writes can
	/// still happen concurrently from other threads.
	/// </summary>
	[ChatService(typeof(IChatStorageLock))]
	public class ChatStorageLock(
		IChatCreationConfig config
	) : IChatStorageLock
	{
		private readonly Lock _lock = new();
		private readonly ChatDatabase _database = config.Database;

		public void Lock()
		{
			_lock.Enter();
		}

		public void Unlock()
		{
			_lock.Exit();
		}

		public void DoTransaction(Action action)
		{
			_lock.Enter();
			try
			{
				if (!_database.Database.BeginTrans())
					throw new InvalidOperationException("Failed to begin transaction.");

				bool committed;
				try
				{
					action();
					committed = _database.Database.Commit();
				}
				catch
				{
					_database.Database.Rollback();
					throw;
				}

				if (!committed)
				{
					_database.Database.Rollback();
					throw new InvalidOperationException("Failed to commit transaction.");
				}
			}
			finally
			{
				_lock.Exit();
			}
		}
	}
}
