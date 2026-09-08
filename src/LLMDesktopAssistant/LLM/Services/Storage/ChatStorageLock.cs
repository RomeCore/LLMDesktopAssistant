using LLMDesktopAssistant.Data;

namespace LLMDesktopAssistant.LLM.Services.Storage
{
	[ChatService(typeof(IChatStorageLock))]
	public class ChatStorageLock(
		ChatDatabase database
	) : IChatStorageLock
	{
		private readonly Lock _lock = new Lock();

		public void Lock()
		{
			_lock.Enter();
		}

		public void Unlock()
		{
			_lock.Exit();
		}

		public bool DoTransaction(Action action)
		{
			if (!database.Database.BeginTrans())
				return false;

			action();

			if (!database.Database.Commit())
			{
				database.Database.Rollback();
				return false;
			}

			return true;
		}
	}
}
