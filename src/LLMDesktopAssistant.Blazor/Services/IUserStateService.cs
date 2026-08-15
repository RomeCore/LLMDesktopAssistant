using LLMDesktopAssistant.Users;

namespace LLMDesktopAssistant.Blazor.Services
{
	public interface IUserStateService
	{
		UserInformation? GetCurrentUser();
	}
}