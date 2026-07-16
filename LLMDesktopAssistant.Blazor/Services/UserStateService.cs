using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Users;
using System.Security.Claims;

namespace LLMDesktopAssistant.Blazor.Services
{
	[WebUIService(typeof(IUserStateService), IsScoped = true)]
	public class UserStateService : IUserStateService
	{
		private readonly IUserManagementService _userManager;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly ILogger<UserStateService> _logger;
		private UserInformation? _userInformation;

		public UserStateService(
			IUserManagementService userManager,
			IHttpContextAccessor httpContextAccessor,
			ILogger<UserStateService> logger)
		{
			_userManager = userManager;
			_httpContextAccessor = httpContextAccessor;
			_logger = logger;
		}

		public UserInformation? GetCurrentUser()
		{
			if (_userInformation != null)
				return _userInformation;

			var login = _httpContextAccessor.HttpContext?.User.Identities
				.FirstOrDefault(i => i.AuthenticationType == WebUIStaticConfiguration.CookiesScheme)?
				.FindFirst(WebUIStaticConfiguration.LoginClaim)?.Value;
			if (login == null)
			{
				_logger.LogWarning("No login found in the current user identity.");
				return null;
			}
			_logger.LogInformation("Got current user with login: {Login}", login);
			_userInformation = _userManager.FindByLogin(login);
			return _userInformation;
		}
	}
}
