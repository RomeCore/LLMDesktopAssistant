using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Localization.Resources;
using LLMDesktopAssistant.Utils;
using LLTSharp;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Statistics;
using Serilog;
using System.Text;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// Service that automatically generates titles and topics for chat conversations.
	/// Uses a dedicated LLM call with a specialized prompt template.
	/// Title and Topic are set directly on the Chat, which is reactive (NotifyPropertyChanged),
	/// so the UI and database are updated automatically through the existing infrastructure.
	/// </summary>
	[ChatService(typeof(IChatNamingService))]
	public class ChatNamingService(
		Chat chat,
		ILLMBuildingService llmBuilder,
		IPromptChatBuilder promptBuilder,
		TemplateLibrary templates,
		IUsageStatsCollector usageStatsCollector,
		MessagesInterface messagesInterface
		) : IChatNamingService
	{
		public async Task TryNameChatAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				if (!IsDefaultTitle(chat.Title))
					return;

				var rounds = messagesInterface.GroupMessagesIntoRounds();
				if (rounds.Count < 1)
					return;

				await NameChatAsync(cancellationToken);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to auto-name chat: {Error}", ex.Message);
			}
		}

		public async Task<bool> NameChatAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				var namingLLM = llmBuilder.BuildSummarizationLLM();
				if (namingLLM == null)
				{
					Log.Warning("Cannot name chat: no naming LLM available (using summarization model fallback).");
					return false;
				}

				Log.Information("Started chat naming process for chat {ChatId}.", chat.ChatId);

				var namingTemplate = (ITextTemplate)templates.Retrieve("naming_prompt")!;
				var namingPrompt = namingTemplate.Render();
				var namingInput = BuildNamingInput();
				IMessage[] messages = [
					new SystemMessage(namingPrompt),
					new RCLargeLanguageModels.Messages.UserMessage(namingInput)
				];

				var timeRequested = DateTime.Now;
				var result = await namingLLM.LLM.ChatAsync(messages);
				var timeFinished = DateTime.Now;

				var content = MarkdownCodeBlockExtractor.TryExtract(result.Content ?? string.Empty);
				if (string.IsNullOrWhiteSpace(content))
				{
					Log.Warning("Chat naming returned empty result.");
					return false;
				}

				string? title = null;
				string? topic = null;

				try
				{
					var json = System.Text.Json.JsonDocument.Parse(content);
					var root = json.RootElement;

					if (root.TryGetProperty("title", out var titleEl) && titleEl.ValueKind == System.Text.Json.JsonValueKind.String)
						title = titleEl.GetString();

					if (root.TryGetProperty("topic", out var topicEl) && topicEl.ValueKind == System.Text.Json.JsonValueKind.String)
						topic = topicEl.GetString();
				}
				catch (System.Text.Json.JsonException)
				{
					title = content.Trim();
				}

				if (!string.IsNullOrWhiteSpace(title))
				{
					chat.Title = title.Trim();
				}

				if (!string.IsNullOrWhiteSpace(topic))
				{
					chat.Topic = topic.Trim();
				}

				var usageMetadata = result.UsageMetadata;
				if (usageMetadata != null)
				{
					usageStatsCollector.RecordUsage(
						model: namingLLM.LLM.Name,
						inputTokens: usageMetadata.InputTokens,
						outputTokens: usageMetadata.OutputTokens,
						durationMs: (long)(timeFinished - timeRequested).TotalMilliseconds,
						success: true);
				}

				Log.Information("Chat named successfully. Title: {Title}, Topic: {Topic}",
					chat.Title, chat.Topic);

				return true;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to name chat: {Error}", ex.Message);

				try
				{
					var namingLLM = llmBuilder.BuildSummarizationLLM();
					if (namingLLM != null)
					{
						usageStatsCollector.RecordUsage(
							model: namingLLM.LLM.Name,
							inputTokens: 0,
							outputTokens: 0,
							durationMs: 0,
							success: false,
							errorMessage: ex.Message);
					}
				}
				catch (Exception recordEx)
				{
					Log.Error(recordEx, "Failed to record usage statistics for failed naming");
				}

				return false;
			}
		}

		private string BuildNamingInput()
		{
			var sb = new StringBuilder();
			sb.AppendLine("Generate a title and topic for this conversation:");

			var rounds = messagesInterface.GroupMessagesIntoRounds(3);
			foreach (var round in rounds)
			{
				foreach (var branched in round)
				{
					var rendered = promptBuilder.RenderMessage(branched.Message);
					if (!string.IsNullOrWhiteSpace(rendered))
					{
						sb.AppendLine(rendered);
					}
				}
			}

			sb.AppendLine();
			sb.AppendLine("---");
			sb.AppendLine("Respond with a JSON object containing 'title' (max 60 chars) and 'topic' (short category like 'coding', 'writing', 'roleplay', 'dnd', etc.).");
			sb.AppendLine("Example: {\"title\": \"Fixing the login bug\", \"topic\": \"coding\"}");

			return sb.ToString();
		}

		private static bool IsDefaultTitle(string title)
		{
			if (string.IsNullOrEmpty(title))
				return true;

			var defaultTitle = LocalizationManager.LocalizeStatic("new_chat");
			if (string.Equals(title, defaultTitle, StringComparison.OrdinalIgnoreCase))
				return true;

			return false;
		}
	}
}
