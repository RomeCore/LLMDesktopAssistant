using System.Security.Claims;
using LLMDesktopAssistant.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace LLMDesktopAssistant.Blazor.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
	private readonly IUserManagementService _userManager;
	private readonly IPasswordHashingService _passwordHasher;
	private readonly WebUIStartupSettings _settings;
	private readonly ILogger<AuthController> _logger;

	public AuthController(
		IUserManagementService userManager,
		IPasswordHashingService passwordHasher,
		WebUIStartupSettings settings,
		ILogger<AuthController> logger)
	{
		_userManager = userManager;
		_passwordHasher = passwordHasher;
		_settings = settings;
		_logger = logger;
	}

	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] AuthRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
			return BadRequest(new { error = "Логин и пароль обязательны" });
		if (_settings.PasswordHash != null && request.MasterPassword == null)
			return BadRequest(new { error = "Мастер-пароль не предоставлен" });

		// Lul... VS thinks that request.MasterPassword can be null down here
		if (_settings.PasswordHash != null && !_passwordHasher.VerifyPassword(_settings.PasswordHash, request.MasterPassword!))
		{
			_logger.LogError("Failed to verify master password for login: {Login}", request.Login);
			return Unauthorized(new { error = "Неверный мастер-пароль" });
		}

		var user = _userManager.FindByLogin(request.Login);
		if (user == null)
		{
			_logger.LogWarning("Login attempt for non-existent user: {Login}", request.Login);
			return Unauthorized(new { error = "Неверный логин или пароль" });
		}

		if (_userManager.IsLocalUser(request.Login))
		{
			_logger.LogWarning("Local user login attempt via WebUI: {Login}", request.Login);
			return Unauthorized(new { error = "Локальные пользователи не могут входить через WebUI" });
		}

		if (!string.IsNullOrEmpty(user.PasswordHash) && 
		    !_passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
		{
			_logger.LogWarning("Failed login attempt for user: {Login}", request.Login);
			return Unauthorized(new { error = "Неверный логин или пароль" });
		}

		var claims = new List<Claim>
		{
			new(ClaimTypes.Name, user.Login),
			new(ClaimTypes.GivenName, user.Name),
			new(WebUIStaticConfiguration.LoginClaim, user.Login),
			new(WebUIStaticConfiguration.PasswordClaim, user.PasswordHash ?? string.Empty),
		};
		if (_settings.PasswordHash != null)
			claims.Add(new(WebUIStaticConfiguration.MasterPasswordClaim, _settings.PasswordHash));

		var identity = new ClaimsIdentity(claims, WebUIStaticConfiguration.CookiesScheme);
		var principal = new ClaimsPrincipal(identity);

		await HttpContext.SignInAsync(WebUIStaticConfiguration.CookiesScheme, principal, new AuthenticationProperties
		{
			IsPersistent = true,
			ExpiresUtc = DateTimeOffset.UtcNow.Add(WebUIStaticConfiguration.LoginExpiryTimeSpan)
		});

		_logger.LogInformation("User logged in: {Login} ({Name})", user.Login, user.Name);
		return Ok(new { name = user.Name, login = user.Login });
	}

	[HttpPost("logout")]
	public async Task<IActionResult> Logout()
	{
		var login = User.FindFirst("Login")?.Value ?? "unknown";
		await HttpContext.SignOutAsync(WebUIStaticConfiguration.CookiesScheme);
		_logger.LogInformation("User logged out: {Login}", login);
		return Ok();
	}

	[HttpGet("me")]
	public IActionResult Me()
	{
		if (User.Identity?.IsAuthenticated != true)
			return Unauthorized(new { error = "Не авторизован" });

		return Ok(new
		{
			name = User.FindFirst(ClaimTypes.GivenName)?.Value ?? User.Identity.Name,
			login = User.FindFirst(WebUIStaticConfiguration.LoginClaim)?.Value ?? User.Identity.Name,
		});
	}

	public record AuthRequest(string Login, string Password, string? MasterPassword);
}
