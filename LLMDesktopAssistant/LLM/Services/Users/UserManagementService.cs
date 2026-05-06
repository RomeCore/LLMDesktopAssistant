using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.Services.Users
{
	[ChatService(typeof(IUserManagementService))]
	public class UserManagementService(
		Chat chat
	) : IUserManagementService
	{
		public UserInformation? FindByLogin(string login)
		{
			return GetAllUsers().FirstOrDefault(u => u.Login == login);
		}

		public IEnumerable<UserInformation> GetAllUsers()
		{
			return chat.Settings.Users.Users;
		}
	}
}