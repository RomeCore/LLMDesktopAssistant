using System;
using System.Collections.Immutable;
using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.Services
{
	[ChatService(typeof(IChatOperationService))]
	public class ChatOperationService(
		Chat chat,
		IChatStorageService storage,
		IChatExecutionService executor
		) : IChatOperationService
	{
		private CancellationTokenSource? _cts = null;

		private void ClearCTS()
		{
			chat.GenerationCts?.Cancel();
			chat.GenerationCts?.Dispose();
			chat.GenerationCts = null;
			_cts?.Cancel();
			_cts?.Dispose();
			_cts = null;
		}

		private CancellationToken UpdateCTS(CancellationToken cancellationToken = default)
		{
			chat.GenerationCts?.Cancel();
			chat.GenerationCts?.Dispose();
			chat.GenerationCts = new CancellationTokenSource();
			_cts?.Cancel();
			_cts?.Dispose();
			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
				chat.GenerationCts.Token);
			return _cts.Token;
		}

		public async Task ContinueGenerationAsync(CancellationToken cancellationToken = default)
		{
			cancellationToken = UpdateCTS(cancellationToken);
			try
			{
				await executor.GenerateResponseAsync(cancellationToken);
			}
			finally
			{
				ClearCTS();
			}
		}

		public async Task SendUserInputAsync(UserInput userInput, bool generate, CancellationToken cancellationToken = default)
		{
			cancellationToken = UpdateCTS(cancellationToken);
			try
			{
				storage.AppendMessage(new UserMessage
				{
					CreatedAt = DateTime.Now,
					Content = userInput.Content,
					SenderLogin = userInput.SenderLogin,
					Attachments = [..userInput.Attachments],
					Visibility = userInput.Visibility,
					VisibleTo = userInput.VisibleTo,
					IsVisibleToWhiteList = false
				});

				if (generate)
					await executor.GenerateResponseAsync(cancellationToken);
			}
			finally
			{
				ClearCTS();
			}
		}

		public async Task SendEditedUserInputAsync(int messageIndex, UserInput userInput, bool generate, CancellationToken cancellationToken = default)
		{
			if (messageIndex < 0 || messageIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(messageIndex));

			cancellationToken = UpdateCTS(cancellationToken);
			try
			{
				storage.EditMessage(messageIndex, new UserMessage
				{
					CreatedAt = DateTime.Now,
					Content = userInput.Content,
					SenderLogin = userInput.SenderLogin,
					Attachments = [.. userInput.Attachments],
					Visibility = userInput.Visibility,
					VisibleTo = userInput.VisibleTo,
					IsVisibleToWhiteList = false
				});

				if (generate)
					await executor.GenerateResponseAsync(cancellationToken);
			}
			finally
			{
				ClearCTS();
			}
		}

		public async Task RegenerateMessageAsync(int messageIndex, CancellationToken cancellationToken = default)
		{
			if (messageIndex < 0 || messageIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(messageIndex));

			cancellationToken = UpdateCTS(cancellationToken);
			try
			{
				var targetMessage = chat.Messages[messageIndex].Message;

				if (targetMessage is UserMessage)
				{
					throw new InvalidOperationException("Cannot regenerate a user message.");
				}
				else if (targetMessage is AssistantMessage)
				{
					if (messageIndex < chat.Messages.Count)
						storage.PlaceNewBranch(messageIndex);
					await executor.GenerateResponseAsync(cancellationToken);
				}
				else
				{
					throw new InvalidOperationException("Invalid message type.");
				}
			}
			finally
			{
				ClearCTS();
			}
		}

		public async Task ResendMessageAsync(int messageIndex, CancellationToken cancellationToken = default)
		{
			if (messageIndex < 0 || messageIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(messageIndex));

			cancellationToken = UpdateCTS(cancellationToken);
			try
			{
				var targetMessage = chat.Messages[messageIndex].Message;

				var nextMessageIndex = messageIndex + 1;
				if (nextMessageIndex < chat.Messages.Count)
					storage.PlaceNewBranch(nextMessageIndex);
				await executor.GenerateResponseAsync(cancellationToken);
			}
			finally
			{
				ClearCTS();
			}
		}

		public void SwitchBranch(int messageIndex, int branchIndex)
		{
			if (messageIndex < 0 || messageIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(messageIndex));
			if (branchIndex < 0 || branchIndex >= chat.Messages[messageIndex].AvailableBranchesCount)
				throw new ArgumentOutOfRangeException(nameof(branchIndex));

			ClearCTS();
			storage.SwitchBranch(messageIndex, branchIndex);
		}

		public void EditMessage(int messageIndex, ChatMessage newMessage)
		{
			if (messageIndex < 0 || messageIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(messageIndex));

			ClearCTS();
			storage.EditMessage(messageIndex, newMessage);
		}

		public void DeleteMessageWithDescendants(int messageIndex)
		{
			if (messageIndex < 0 || messageIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(messageIndex));

			ClearCTS();
			storage.DeleteMessageWithDescendants(messageIndex);
		}
	}
}
