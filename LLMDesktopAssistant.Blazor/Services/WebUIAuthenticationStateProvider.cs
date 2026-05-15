using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.WebUI;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace LLMDesktopAssistant.Blazor.Services;

public class WebUIAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
	private readonly IUserManagementService _userManager;
	private readonly IPasswordHashingService _passwordHasher;
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly RemoteUsersConfiguration _remoteConfig;
	private readonly ILogger<WebUIAuthenticationStateProvider> _logger;
	private readonly ReaderWriterLockSlim _lock = new();

	private ClaimsPrincipal? _currentUser;

	public WebUIAuthenticationStateProvider(
		IUserManagementService userManager,
		IPasswordHashingService passwordHasher,
		ILogger<WebUIAuthenticationStateProvider> logger,
		IHttpContextAccessor httpContextAccessor)
	{
		_userManager = userManager;
		_passwordHasher = passwordHasher;
		_httpContextAccessor = httpContextAccessor;
		_remoteConfig = SettingsManager.Get<RemoteUsersConfiguration>();
		_logger = logger;
	}

	private ClaimsPrincipal GetUserClaims()
	{
		if (_currentUser != null)
			return _currentUser;

		var httpContext = _httpContextAccessor.HttpContext;
		if (httpContext?.User.Identity?.IsAuthenticated == true)
		{
			_currentUser = httpContext.User;
			_logger.LogInformation("Session restored from cookie for user: {User}",
				_currentUser.Identity?.Name);
		}

		return _currentUser ?? new ClaimsPrincipal(new ClaimsIdentity());
	}

	public override Task<AuthenticationState> GetAuthenticationStateAsync()
	{
		_lock.EnterReadLock();
		try
		{
			return Task.FromResult(new AuthenticationState(GetUserClaims()));
		}
		finally
		{
			_lock.ExitReadLock();
		}
	}

	public async Task<bool> RegisterAsync(string login, string password, string name, string description = "")
	{
		if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
			return false;

		if (_userManager.FindByLogin(login) != null)
		{
			_logger.LogWarning("Registration attempt with existing login: {Login}", login);
			return false;
		}

		var hash = _passwordHasher.HashPassword(password);
		var newUser = new UserInformation
		{
			Login = login,
			Name = name,
			PasswordHash = hash,
			Description = description
		};

		_remoteConfig.Users.Add(newUser);

		_logger.LogInformation("New user registered: {Login} ({Name})", login, name);
		return true;
	}

	public UserInformation? GetCurrentUser()
	{
		var login = GetUserClaims().FindFirst("Login")?.Value;
		if (login == null) return null;
		return _userManager.FindByLogin(login);
	}

	public void Dispose()
	{
		_lock?.Dispose();
	}
}
