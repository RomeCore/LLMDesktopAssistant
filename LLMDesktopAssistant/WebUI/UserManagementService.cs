using DocumentFormat.OpenXml.Wordprocessing;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Settings;
using Serilog;

namespace LLMDesktopAssistant.WebUI
{
	[ChatService(typeof(IUserManagementService))]
	public class UserManagementService(
		Chat chat,
		IPasswordHashingService hasher
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

		public UserInformation? RegisterUser(string login, string password, string? name, string? description)
		{
			if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
				return null;

			if (FindByLogin(login) != null)
			{
				Log.Warning("Registration attempt with existing login: {Login}", login);
				return null;
			}

			var hash = hasher.HashPassword(password);
			var newUser = new UserInformation
			{
				Login = login,
				Name = name ?? login,
				PasswordHash = hash,
				Description = description ?? string.Empty
			};

			SettingsManager.Get<RemoteUsersConfiguration>().Users.Add(newUser);

			Log.Information("New user registered: {Login} ({Name})", login, newUser.Name);
			return newUser;
		}
	}
}