using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Blazor.Services
{
	public interface IOnlineStateService
	{
		ReadOnlyObservableCollection<string> OnlineUsers { get; }

		void EnterSession(string login);
		void LeaveSession(string login);
	}
}