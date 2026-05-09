using LLMDesktopAssistant.WebUI;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Settings;

namespace LLMDesktopAssistant.LLM.Services.Users
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