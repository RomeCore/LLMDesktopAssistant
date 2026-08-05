using System.ComponentModel;
using System.Text;
using Avalonia.Threading;
using LLMDesktopAssistant.Agents.Tasks;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM.Additional.Context;
using LLMDesktopAssistant.LLM.Services.Prompting;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Providers;
using LLTSharp;
using RCLargeLanguageModels.Metadata;
using Serilog;

namespace LLMDesktopAssistant.LLM.Services
{
	[ChatService(typeof(IChatSummarizationService))]
	public class ChatSummarizationService(
		Chat chat,
		IChatPromptBuilder promptBuilder,
		TemplateLibrary templates,
		IAgentTaskExecutor agentTaskExecutor,
		IModelManager modelManager
		) : IChatSummarizationService
	{
		public async Task TrySummarizeChatAsync(IUsageMetadata lastUsageMetadata, CancellationToken cancellationToken = default)
		{
			var summarizationLLM = modelManager.TryGetModel(chat.Settings.Summarization.GetEffectiveOptions().SummarizerModel);

			try
			{
				if (!chat.Settings.Summarization.AutoSummarizationEnabled)
					return;

				var totalTokensUsed = lastUsageMetadata.TotalTokens;
				if (totalTokensUsed < chat.Settings.Summarization.GetEffectiveOptions().SummarizationTriggerTokens)
					return;

				// If the summarization LLM is not available, do not summarize
				if (summarizationLLM == null)
					return;

				var lastRoundsToIgnore = chat.Settings.Summarization.GetEffectiveOptions().IgnoreLastRounds;

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
			var summarizationLLM = modelManager.TryGetModel(chat.Settings.Summarization.GetEffectiveOptions().SummarizerModel);

			try
			{
				// If the summarization LLM is not available, do not summarize
				if (summarizationLLM == null)
					return;

				Log.Information("Started summarization process.");

				var summarizerTemplate = (ITextTemplate)templates.Retrieve("summarization_prompt")!;
				var summarizerPrompt = summarizerTemplate.Render();
				var summarizerInput = BuildSummarizerInput(message);

				var summarizationTask = agentTaskExecutor.Execute(new AgentTaskLaunchParameters
				{
					TaskName = LocalizationManager.LocalizeStatic("chat_summarization_task"),
					TriggeredChat = chat,
					Model = summarizationLLM,
					InitialMessages = [
						new AgentSystemMessage { Content = summarizerPrompt },
						new AgentUserMessage { Content = summarizerInput }
					]
				}, cancellationToken);

				var viewModel = new SummaryViewModel
				{
					Summary = summarizationTask.LastGeneratedContent ?? string.Empty,
					Completed = false
				};
				message.AdditionalViewModels.TryReplace(viewModel);
				PropertyChangedEventHandler summaryChanged = (s, e) =>
				{
					if (e.PropertyName is nameof(summarizationTask.LastGeneratedContent))
						Dispatcher.UIThread.Post(() =>
						{
							viewModel.Summary = summarizationTask.LastGeneratedContent ?? string.Empty;
						});
				};
				summarizationTask.PropertyChanged += summaryChanged;

				await summarizationTask;
				summarizationTask.PropertyChanged -= summaryChanged;
				viewModel.Completed = true;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to summarize chat: {Error}", ex.Message);
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
						parts.Insert(0, promptBuilder.RenderMessage(message));
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
					parts.Insert(0, promptBuilder.RenderMessage(message));
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
