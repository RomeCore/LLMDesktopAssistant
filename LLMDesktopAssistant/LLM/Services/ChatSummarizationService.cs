using Avalonia.Threading;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM.Additional.Context;
using LLTSharp;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Metadata;
using RCLargeLanguageModels.Statistics;
using Serilog;
using System.Text;

namespace LLMDesktopAssistant.LLM.Services
{
	[ChatService(typeof(IChatSummarizationService))]
	public class ChatSummarizationService(
		Chat chat,
		ILLMBuildingService llmBuilder,
		IPromptChatBuilder promptBuilder,
		IMessageTokenSerializationSchema messageSerializer,
		TemplateLibrary templates,
		IUsageStatsCollector usageStatsCollector
		) : IChatSummarizationService
	{
		public async Task TrySummarizeChatAsync(IUsageMetadata lastUsageMetadata, CancellationToken cancellationToken = default)
		{
			try
			{
				if (!chat.Settings.Summarization.AutoSummarizationEnabled)
					return;

				var totalTokensUsed = lastUsageMetadata.TotalTokens;
				if (totalTokensUsed < chat.Settings.Summarization.SummarizationTriggerTokens)
					return;

				var summarizationLLM = llmBuilder.BuildSummarizationLLM();
				// If the summarization LLM is not available, do not summarize
				if (summarizationLLM == null)
					return;

				var lastRoundsToIgnore = chat.Settings.Summarization.IgnoreLastRounds;

				var rounds = GroupMessagesIntoRounds(chat.Messages.Select(m => m.Message).ToList());
				var roundsToSummarize = rounds.Take(Math.Max(0, rounds.Count - lastRoundsToIgnore)).ToList();
				if (roundsToSummarize.Count == 0)
					return;

				var lastMessageInRange = roundsToSummarize.Last().Last();
				await SummarizeMessageWithPreviousMessagesAsync(lastMessageInRange, cancellationToken);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to summarize chat: {Error}", ex.Message);

				try
				{
					var summarizationLLM = llmBuilder.BuildSummarizationLLM();
					if (summarizationLLM != null)
					{
						usageStatsCollector.RecordUsage(
							model: summarizationLLM.LLM.Name,
							inputTokens: 0,
							outputTokens: 0,
							durationMs: 0,
							success: false,
							errorMessage: ex.Message);
					}
				}
				catch (Exception recordEx)
				{
					Log.Error(recordEx, "Failed to record usage statistics for failed summarization");
				}
			}
		}

		/// <summary>
		/// Groups messages into rounds.
		/// A round = [one or more consecutive user messages] + [one or more consecutive assistant messages].
		/// </summary>
		private static List<List<ChatMessage>> GroupMessagesIntoRounds(IReadOnlyList<ChatMessage> messages)
		{
			var rounds = new List<List<ChatMessage>>();
			if (messages.Count == 0)
				return rounds;

			List<ChatMessage>? currentRound = null;
			bool? lastWasUser = null;

			foreach (var branched in messages)
			{
				bool isUser = branched is Domain.UserMessage;
				bool isAssistant = branched is Domain.AssistantMessage;

				if (isUser)
				{
					// Start a new round if previous was assistant, or first message is user
					if (lastWasUser == false || lastWasUser == null)
					{
						currentRound = [branched];
						rounds.Add(currentRound);
					}
					else
					{
						currentRound?.Add(branched);
					}
					lastWasUser = true;
				}
				else if (isAssistant)
				{
					if (lastWasUser == true || lastWasUser == null)
					{
						currentRound = [branched];
						rounds.Add(currentRound);
					}
					else
					{
						currentRound?.Add(branched);
					}
					lastWasUser = false;
				}
			}

			return rounds;
		}

		public async Task SummarizeMessageWithPreviousMessagesAsync(ChatMessage message, CancellationToken cancellationToken = default)
		{
			try
			{
				var summarizationLLM = llmBuilder.BuildSummarizationLLM();
				// If the summarization LLM is not available, do not summarize
				if (summarizationLLM == null)
					return;

				Log.Information("Started summarization process.");

				var summarizerTemplate = (ITextTemplate)templates.Retrieve("summarization_prompt")!;
				var summarizerPrompt = summarizerTemplate.Render();
				var summarizerInput = BuildSummarizerInput(message);
				IMessage[] messages = [
					new SystemMessage(summarizerPrompt),
					new RCLargeLanguageModels.Messages.UserMessage(summarizerInput)
				];

				var timeRequested = DateTime.Now;
				var summary = await summarizationLLM.LLM.ChatStreamingAsync(messages);
				var timeFinished = DateTime.Now;

				var viewModel = new SummaryViewModel
				{
					Summary = summary.Content ?? string.Empty,
					Completed = false
				};
				message.AdditionalViewModels.TryReplace(viewModel);
				EventHandler<AssistantMessageDelta> summaryPartAdded = (s, e) =>
				{
					Dispatcher.UIThread.Post(() =>
					{
						viewModel.Summary = summary.Content ?? string.Empty;
					});
				};
				summary.Message.PartAdded += summaryPartAdded;

				await summary;
				summary.Message.PartAdded -= summaryPartAdded;
				viewModel.Completed = true;

				var summaryUsageMetadata = summary.UsageMetadata;
				if (summaryUsageMetadata != null)
				{
					if (summaryUsageMetadata is IUsageCacheMetadata usageCacheMetadata)
					{
						usageStatsCollector.RecordUsage(
							model: summarizationLLM.LLM.Name,
							inputTokens: summaryUsageMetadata.InputTokens,
							outputTokens: summaryUsageMetadata.OutputTokens,
							cacheHitTokens: usageCacheMetadata.InputCacheHitTokens,
							cacheMissTokens: usageCacheMetadata.InputCacheMissTokens,
							durationMs: (long)(timeFinished - timeRequested).TotalMilliseconds,
							success: true);
					}
					else
					{
						usageStatsCollector.RecordUsage(
							model: summarizationLLM.LLM.Name,
							inputTokens: summaryUsageMetadata.InputTokens,
							outputTokens: summaryUsageMetadata.OutputTokens,
							durationMs: (long)(timeFinished - timeRequested).TotalMilliseconds,
							success: true);
					}
				}


				Log.Information("Chat summarized successfully. Summary length: {Length}, Summary: {Summary}",
					summary.Content?.Length, summary.Content);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to summarize chat: {Error}", ex.Message);

				try
				{
					var summarizationLLM = llmBuilder.BuildSummarizationLLM();
					if (summarizationLLM != null)
					{
						usageStatsCollector.RecordUsage(
							model: summarizationLLM.LLM.Name,
							inputTokens: 0,
							outputTokens: 0,
							durationMs: 0,
							success: false,
							errorMessage: ex.Message);
					}
				}
				catch (Exception recordEx)
				{
					Log.Error(recordEx, "Failed to record usage statistics for failed summarization");
				}
			}
		}

		private string BuildSummarizerInput(ChatMessage targetMessage)
		{
			var sb = new StringBuilder();
			sb.AppendLine("Summarize this conversation:");

			var parts = new List<string>();
			string? latestSummary = null;
			bool encounteredUserMessage = false, foundTargetMessage = false;

			for (int i = chat.Messages.Count - 1; i >= 0; i--)
			{
				var message = chat.Messages[i].Message;

				if (!foundTargetMessage)
				{
					if (targetMessage == message)
						foundTargetMessage = true;
					else
						continue;
				}

				if (message.AdditionalViewModels.Has<ContextShieldViewModel>())
					break;

				if (message is Domain.UserMessage userMessage)
				{
					encounteredUserMessage = true;
					if (latestSummary != null)
					{
						parts.Insert(0, string.Join(Environment.NewLine, promptBuilder.ConvertMessageUnsafe(message)
							.Select(m => messageSerializer.SerializeMessage(m, []))));
						break;
					}
				}

				if (targetMessage != message &&
					message.AdditionalViewModels.TryGet<SummaryViewModel>(out var summaryViewModel) &&
					summaryViewModel.Completed)
				{
					latestSummary = summaryViewModel.Summary;
					if (encounteredUserMessage)
						break;
				}

				if (latestSummary == null)
				{
					parts.Insert(0, string.Join(Environment.NewLine, promptBuilder.ConvertMessageUnsafe(message)
						.Select(m => messageSerializer.SerializeMessage(m, []))));
				}
			}

			if (!foundTargetMessage)
				throw new InvalidOperationException("Target message not found in chat history.");

			sb.Append("Latest summary: ").AppendLine(latestSummary);
			foreach (var part in parts)
				sb.AppendLine(part);

			return sb.ToString();
		}
	}
}
