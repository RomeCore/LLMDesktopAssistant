using LLMDesktopAssistant.Core.LLM.Domain;

namespace LLMDesktopAssistant.Core.LLM.Services
{
	public interface IChatOperationService
	{
		Task ContinueGenerationAsync(CancellationToken cancellationToken = default);
		Task SendUserInputAsync(UserInput userInput, CancellationToken cancellationToken = default);
		Task SendEditedUserInputAsync(int messageIndex, UserInput userInput, CancellationToken cancellationToken = default);
		Task ResendMessageAsync(int messageIndex, CancellationToken cancellationToken = default);
		Task RegenerateMessageAsync(int messageIndex, CancellationToken cancellationToken = default);
		void SwitchBranch(int messageIndex, int branchIndex);
	}
}