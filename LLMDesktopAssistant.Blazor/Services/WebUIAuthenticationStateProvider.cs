using System.Security.Claims;
using LLMDesktopAssistant.WebUI;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

namespace LLMDesktopAssistant.Blazor.Services;

public class WebUIAuthenticationStateProvider(
	ILogger<WebUIAuthenticationStateProvider> logger,
	IHttpContextAccessor httpContextAccessor,
	WebUIStartupSettings settings
) : AuthenticationStateProvider
{
	private ClaimsPrincipal? _currentUser;

	private ClaimsPrincipal GetUserClaims()
	{
		if (_currentUser != null)
			return _currentUser;

		var httpContext = httpContextAccessor.HttpContext;
		if (httpContext?.User.Identity?.IsAuthenticated == true)
		{
			_currentUser = httpContext.User;
			logger.LogInformation("Session restored from cookie for user: {User}",
				_currentUser.Identity?.Name);
		}

		return _currentUser ?? new ClaimsPrincipal(new ClaimsIdentity());
	}

	public override Task<AuthenticationState> GetAuthenticationStateAsync()
	{
		return Task.FromResult(new AuthenticationState(GetUserClaims()));
	}

	public void NotifyAuthState()
	{
		_currentUser = null;
		NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
	}

	public bool HasMasterAccess()
	{
		var httpContext = httpContextAccessor.HttpContext;
		if (httpContext == null)
			return false;

		var masterResult = httpContext.AuthenticateAsync(WebUIStaticConfiguration.MasterCookiesScheme).Result;
		return masterResult.Succeeded && masterResult.Principal?.Identity?.IsAuthenticated == true;
	}

	public bool HasLoginAccess()
	{
		var httpContext = httpContextAccessor.HttpContext;
		if (httpContext == null)
			return false;

		var loginResult = httpContext.AuthenticateAsync(WebUIStaticConfiguration.LoginCookiesScheme).Result;
		return loginResult.Succeeded && loginResult.Principal?.Identity?.IsAuthenticated == true;
	}

	public bool CanAccessChat()
	{
		if (string.IsNullOrEmpty(settings.PasswordHash))
			return HasLoginAccess();

		return HasMasterAccess() || HasLoginAccess();
	}
}
