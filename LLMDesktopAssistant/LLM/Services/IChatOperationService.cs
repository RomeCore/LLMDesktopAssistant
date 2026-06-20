using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.Services
{
	public interface IChatOperationService
	{
		Task ContinueGenerationAsync(CancellationToken cancellationToken = default);
		Task SendUserInputAsync(UserInput userInput, bool generate, CancellationToken cancellationToken = default);
		Task SendEditedUserInputAsync(int messageIndex, UserInput userInput, bool generate, CancellationToken cancellationToken = default);
		Task ResendMessageAsync(int messageIndex, CancellationToken cancellationToken = default);
		Task RegenerateMessageAsync(int messageIndex, CancellationToken cancellationToken = default);
		void SwitchBranch(int messageIndex, int branchIndex);
		void EditMessage(int messageIndex, ChatMessage newMessage);
		void DeleteMessageWithDescendants(int messageIndex);
	}
}