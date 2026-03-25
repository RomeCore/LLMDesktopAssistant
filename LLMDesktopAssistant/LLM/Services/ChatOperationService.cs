using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.Services
{
	public class ChatOperationService(
		Chat chat,
		IChatStorageService storage,
		IChatExecutionService executor
		) : IChatOperationService
	{
		private CancellationTokenSource? _cts = null;

		public async Task ContinueGenerationAsync(CancellationToken cancellationToken = default)
		{
			_cts?.Cancel();
			_cts?.Dispose();

			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cancellationToken = _cts.Token;

			await executor.GenerateResponseAsync(cancellationToken);
		}

		public async Task SendUserInputAsync(UserInput userInput, CancellationToken cancellationToken = default)
		{
			_cts?.Cancel();
			_cts?.Dispose();

			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cancellationToken = _cts.Token;

			storage.AppendMessage(new UserMessage
			{
				Content = userInput.Content,
			});
			await executor.GenerateResponseAsync(cancellationToken);
		}

		public async Task SendEditedUserInputAsync(int messageIndex, UserInput userInput, CancellationToken cancellationToken = default)
		{
			if (messageIndex < 0 || messageIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(messageIndex));

			_cts?.Cancel();
			_cts?.Dispose();

			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cancellationToken = _cts.Token;

			storage.EditMessage(messageIndex, new UserMessage
			{
				Content = userInput.Content,
			});
			await executor.GenerateResponseAsync(cancellationToken);
		}

		public async Task RegenerateOrResendMessageAsync(int messageIndex, CancellationToken cancellationToken = default)
		{
			if (messageIndex < 0 || messageIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(messageIndex));

			_cts?.Cancel();
			_cts?.Dispose();

			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cancellationToken = _cts.Token;

			var targetMessage = chat.Messages[messageIndex].Message;

			// If the target message is a user message, resend it
			if (targetMessage is UserMessage)
			{
				var nextMessageIndex = messageIndex + 1;
				if (nextMessageIndex < chat.Messages.Count)
					storage.PlaceNewBranch(nextMessageIndex);
				await executor.GenerateResponseAsync(cancellationToken);
			}
			// If the target message is an assistant message, regenerate it
			else if (targetMessage is AssistantMessage)
			{
				if (messageIndex < chat.Messages.Count)
					storage.PlaceNewBranch(messageIndex);
				await executor.GenerateResponseAsync(cancellationToken);
			}
		}

		public void SwitchBranch(int messageIndex, int branchIndex)
		{
			if (messageIndex < 0 || messageIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(messageIndex));
			if (branchIndex < 0 || branchIndex >= chat.Messages[messageIndex].AvailableBranchesCount)
				throw new ArgumentOutOfRangeException(nameof(branchIndex));

			_cts?.Cancel();
			_cts?.Dispose();
			_cts = null;

			storage.SwitchBranch(messageIndex, branchIndex);
		}
	}
}