using LLMDesktopAssistant.Utils;
using Serilog;

namespace LLMDesktopAssistant.Blazor.Services
{
	[WebUIService(typeof(IOnlineStateService), IsScoped = false)]
	public class OnlineStateService : IOnlineStateService
	{
		private readonly object _lock = new();
		private readonly Dictionary<string, int> _sessionCounters = [];

		private readonly RangeObservableCollection<string> _onlinePlayers = [];
		public ReadOnlyObservableCollection<string> OnlineUsers { get; }

		public OnlineStateService()
		{
			OnlineUsers = new(_onlinePlayers);
		}

		public void EnterSession(string login)
		{
			lock (_lock)
			{
				if (!_sessionCounters.TryGetValue(login, out int counter))
					_onlinePlayers.Add(login);
				_sessionCounters[login] = ++counter;
			}
		}

		public void LeaveSession(string login)
		{
			lock (_lock)
			{
				if (!_sessionCounters.TryGetValue(login, out int counter))
				{
					Log.Warning("User {Login} is trying to leave a session but was not in one.", login);
					return;
				}
				if (counter == 1)
				{
					_sessionCounters.Remove(login);
					_onlinePlayers.Remove(login);
				}
				else
				{
					_sessionCounters[login] = --counter;
				}
			}
		}
	}
}
