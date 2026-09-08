namespace LLMDesktopAssistant.LLM.Services.Storage
{
	public interface IChatStorageLock
	{
		void Lock();

		void Unlock();

		bool DoTransaction(Action action);
	}
}
