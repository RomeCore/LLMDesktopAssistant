using LLMDesktopAssistant.Core.LLM.Domain;
using LLTSharp;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Metadata;
using RCLargeLanguageModels.Statistics;
using Serilog;
using System.Text;

namespace LLMDesktopAssistant.Core.LLM.Services
{
	public class ChatSummarizationService(
		Chat chat,
		ILLMBuildingService llmBuilder,
		IPromptChatBuilder promptBuilder,
		IMessageTokenSerializationSchema messageSerializer,
		TemplateLibrary templates,
		IUsageStatsCollector usageStatsCollector
		) : IChatSummarizationService
	{
		/// <summary>
		/// The threshold to use when determining if a chat should be summarized or not.
		/// </summary>
		private const float Threshold = 0.75f;

		/// <summary>
		/// The number of last messages to not be inculded in the summary.
		/// </summary>
		private const int IgnoreLastMessagesCount = 4;

		public async Task TrySummarizeChat(LLMInfo usedLLM, IUsageMetadata lastUsageMetadata)
		{
			try
			{
				var modelContextLength = usedLLM.ContextSize;
				var totalTokensUsed = lastUsageMetadata.TotalTokens;
				// If the total tokens used is less than Threshold% of the model's context size, do not summarize
				if (totalTokensUsed < modelContextLength * Threshold)
					return;

				var summarizationLLM = llmBuilder.BuildSummarizationLLM();
				// If the summarization LLM is not available, do not summarize
				if (summarizationLLM == null)
					return;
				Log.Information("Started summarization process.");

				var summarizerTemplate = (ITextTemplate)templates.Retrieve("summarization_prompt")!;
				var summarizerPrompt = summarizerTemplate.Render();
				var summarizerInput = BuildSummarizerInput(out var lastIncludedMessage);
				IMessage[] messages = [
					new SystemMessage(summarizerPrompt),
					new RCLargeLanguageModels.Messages.UserMessage(summarizerInput)
				];

				if (lastIncludedMessage is null)
					throw new InvalidOperationException("No messages were included in the summarizer input.");

				var timeRequested = DateTime.Now;
				var summary = await summarizationLLM.LLM.ChatAsync(messages);
				var timeFinished = DateTime.Now;

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

				lastIncludedMessage.SummaryOfPrevMessages = summary.Content;
				Log.Information("Chat summarized successfully. Summary length: {Length}", summary.Content?.Length);
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

		private string BuildSummarizerInput(out ChatMessage? lastIncludedMessage)
		{
			var sb = new StringBuilder();
			sb.AppendLine("Summarize this conversation:");

			var parts = new List<string>();
			string? latestSummary = null;
			bool encounteredUserMessage = false;
			lastIncludedMessage = null;

			for (int i = chat.Messages.Count - 1 - IgnoreLastMessagesCount; i >= 0; i--)
			{
				var message = chat.Messages[i].Message;
				lastIncludedMessage ??= message;

				if (message is Domain.UserMessage userMessage)
				{
					encounteredUserMessage = true;
					if (latestSummary != null)
					{
						parts.Insert(0, string.Join(Environment.NewLine, promptBuilder.ConvertMessage(message)
							.Select(m => messageSerializer.SerializeMessage(m, []))));
						break;
					}
				}

				if (!string.IsNullOrWhiteSpace(message.SummaryOfPrevMessages))
				{
					latestSummary = message.SummaryOfPrevMessages;
					if (encounteredUserMessage)
						break;
				}

				if (latestSummary == null)
				{
					parts.Insert(0, string.Join(Environment.NewLine, promptBuilder.ConvertMessage(message)
						.Select(m => messageSerializer.SerializeMessage(m, []))));
				}
			}

			sb.Append("Latest summary: ").AppendLine(latestSummary);
			foreach (var part in parts)
				sb.AppendLine(part);

			return sb.ToString();
		}
	}
}