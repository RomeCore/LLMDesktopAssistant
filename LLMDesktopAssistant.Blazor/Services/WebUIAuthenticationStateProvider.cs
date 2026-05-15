using System.Security.Claims;
using LLMDesktopAssistant.WebUI;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace LLMDesktopAssistant.Blazor.Services;

public class WebUIAuthenticationStateProvider(
	IHttpContextAccessor httpContextAccessor,
	WebUIStartupSettings settings,
	IUserManagementService userManager,
	// IJSRuntime js,
	ILogger<WebUIAuthenticationStateProvider> logger
) : AuthenticationStateProvider
{
	private ClaimsPrincipal? _currentUser;

	private async Task<ClaimsPrincipal> LogoutUserAsync()
	{
		// await js.InvokeVoidAsync("logoutUser");
		return new ClaimsPrincipal(new ClaimsIdentity());
	}

	private async Task<ClaimsPrincipal> GetUserClaimsAsync()
	{
		if (_currentUser != null)
			return _currentUser;

		var httpContext = httpContextAccessor.HttpContext;
		if (httpContext == null)
			return await LogoutUserAsync();

		var user = httpContext.User;
		if (user.Identity?.IsAuthenticated != true)
			return await LogoutUserAsync();

		var login = user.FindFirst(WebUIStaticConfiguration.LoginClaim)?.Value;
		if (login == null)
			return await LogoutUserAsync();

		if (userManager.FindByLogin(login) is not UserInformation userInfo)
			return await LogoutUserAsync();

		var passwordHash = user.FindFirst(WebUIStaticConfiguration.PasswordClaim)?.Value;
		if (passwordHash != userInfo.PasswordHash)
			return await LogoutUserAsync();

		var masterPasswordHash = user.FindFirst(WebUIStaticConfiguration.MasterPasswordClaim)?.Value;
		if (settings.PasswordHash != null && masterPasswordHash != settings.PasswordHash)
			return await LogoutUserAsync();

		_currentUser = user;
		logger.LogInformation("Session restored from login cookie for user: {User}",
			_currentUser.Identity?.Name);
		return _currentUser;
	}

	public override async Task<AuthenticationState> GetAuthenticationStateAsync()
	{
		return new AuthenticationState(await GetUserClaimsAsync());
	}

	public void NotifyAuthState()
	{
		_currentUser = null;
		NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
	}
}
