using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.Services
{
	public interface IChatOperationService
	{
		Task ContinueGenerationAsync(CancellationToken cancellationToken = default);
		Task SendUserInputAsync(UserInput userInput, CancellationToken cancellationToken = default);
		Task SendEditedUserInputAsync(int messageIndex, UserInput userInput, CancellationToken cancellationToken = default);
		Task RegenerateOrResendMessageAsync(int messageIndex, CancellationToken cancellationToken = default);
		void SwitchBranch(int messageIndex, int branchIndex);
	}
}