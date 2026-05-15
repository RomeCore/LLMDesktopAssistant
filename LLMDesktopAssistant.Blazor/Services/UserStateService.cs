using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.WebUI;
using System.Security.Claims;

namespace LLMDesktopAssistant.Blazor.Services
{
	public class UserStateService
	{
		private readonly IUserManagementService _userManager;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly RemoteUsersConfiguration _remoteConfig;

		public UserStateService(
			IUserManagementService userManager,
			IHttpContextAccessor httpContextAccessor)
		{
			_userManager = userManager;
			_httpContextAccessor = httpContextAccessor;
			_remoteConfig = SettingsManager.Get<RemoteUsersConfiguration>();
		}

		public UserInformation? GetCurrentUser()
		{
			var login = _httpContextAccessor.HttpContext?.User.FindFirst("Login")?.Value;
			if (login == null) return null;
			return _userManager.FindByLogin(login);
		}
	}
}