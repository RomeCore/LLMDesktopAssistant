using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.LLM.Services;

namespace LLMDesktopAssistant.WebUI
{
	[ChatService(typeof(IUserManagementService))]
	public class UserManagementService(
		Chat chat
	) : IUserManagementService
	{
		public IEnumerable<UserInformation> GetAllUsers()
		{
			return GetLocalUsers().Concat(GetRemoteUsers());
		}

		public IEnumerable<UserInformation> GetLocalUsers()
		{
			return chat.Settings.Users.Users;
		}

		public IEnumerable<UserInformation> GetRemoteUsers()
		{
			return SettingsManager.Get<RemoteUsersConfiguration>().Users;
		}

		public bool IsLocalUser(string userLogin)
		{
			return chat.Settings.Users.Users.Any(u => u.Login == userLogin);
		}

		public IEnumerable<UserInformation> GetActiveUsers()
		{
			return GetLocalUsers().Where(u => u.IsLocallyActive);
		}

		public UserInformation? FindByLogin(string login)
		{
			return GetAllUsers().FirstOrDefault(u => u.Login == login);
		}
	}
}