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
	private Timer? _sessionTimer;
	private DateTime _lastActivity = DateTime.UtcNow;
	private readonly TimeSpan _sessionTimeout = WebUIStaticConfiguration.AuthExpiryTimeSpan;

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

		_sessionTimer = new Timer(_ => CheckSession(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
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

		_lastActivity = DateTime.UtcNow;
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

	public async Task<bool> LoginAsync(string login, string password)
	{
		var user = _userManager.FindByLogin(login);
		if (user == null)
		{
			_logger.LogWarning("Login attempt for non-existent user: {Login}", login);
			return false;
		}
		if (_userManager.IsLocalUser(login))
		{
			_logger.LogWarning("Remote login attempt without password for user: {Login}", login);
			return false;
		}

		if (!string.IsNullOrEmpty(user.PasswordHash) && !_passwordHasher.VerifyPassword(user.PasswordHash, password))
		{
			_logger.LogWarning("Failed login attempt for user: {Login}", login);
			return false;
		}

		var claims = new List<Claim>
		{
			new(ClaimTypes.Name, user.Login),
			new(ClaimTypes.GivenName, user.Name),
			new("Login", user.Login),
		};

		var identity = new ClaimsIdentity(claims, "WebUIAuth");
		var principal = new ClaimsPrincipal(identity);

		_lock.EnterWriteLock();
		try
		{
			_currentUser = principal;
			_lastActivity = DateTime.UtcNow;
		}
		finally
		{
			_lock.ExitWriteLock();
		}

		NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));

		_logger.LogInformation("User logged in: {Login} ({Name})", login, user.Name);
		return true;
	}

	public void Logout()
	{
		_lock.EnterWriteLock();
		try
		{
			_currentUser = new ClaimsPrincipal(new ClaimsIdentity());
		}
		finally
		{
			_lock.ExitWriteLock();
		}

		NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
		_logger.LogInformation("User logged out");
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

	private void CheckSession()
	{
		if (!GetUserClaims().Identity?.IsAuthenticated ?? false)
			return;

		if (DateTime.UtcNow - _lastActivity > _sessionTimeout)
		{
			_logger.LogInformation("Session timeout for user: {User}", GetUserClaims().Identity?.Name);
			Logout();
		}
	}

	public void Dispose()
	{
		_sessionTimer?.Dispose();
		_lock?.Dispose();
	}
}
