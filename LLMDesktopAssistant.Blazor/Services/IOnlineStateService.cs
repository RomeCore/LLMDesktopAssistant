using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Blazor.Services
{
	public interface IOnlineStateService
	{
		ReadOnlyObservableCollection<string> OnlinePlayers { get; }

		void EnterSession(string login);
		void LeaveSession(string login);
	}
}