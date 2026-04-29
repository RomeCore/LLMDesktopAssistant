using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Prompting;
using LLTSharp;
using LLTSharp.Locale;
using LLTSharp.Metadata;
using LLTSharp.Metadata.Types;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;
using System.Globalization;
using System.Text;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// Builds the prompt context for a given agent, respecting:
	/// - <see cref="AgentReadSettings.ReadPermissions"/> — what the agent can see
	/// - <see cref="AgentReadSettings.AgentIdsReadFilter"/> — white/black list for other agents
	/// - <see cref="ChatEnvironmentSettings.MaxVisibleRounds"/> — how many recent rounds to include
	/// - Foreign agent messages are merged and presented as a single user message.
	/// </summary>
	[ChatService(typeof(IPromptChatBuilder))]
	public class PromptChatBuilder(
		Chat chat,
		TemplateLibrary templates,
		IAgentManagementService agentSettings
		) : IPromptChatBuilder
	{
		private ToolResultStatus ConvertToolStatus(ToolStatus status)
		{
			return status switch
			{
				ToolStatus.None => ToolResultStatus.NoResult,
				ToolStatus.WaitingForApproval => ToolResultStatus.NoResult,
				ToolStatus.Executing => ToolResultStatus.NoResult,
				ToolStatus.Success => ToolResultStatus.Success,
				ToolStatus.Error => ToolResultStatus.Error,
				ToolStatus.Cancelled => ToolResultStatus.Cancelled,
				_ => throw new ArgumentOutOfRangeException(nameof(status), status, "Invalid status."),
			};
		}

		private LanguageMetadata GetCurrentLanguageMetadata()
		{
			return new LanguageMetadata(new LanguageCode(CultureInfo.CurrentCulture));
		}

		private string BuildSystemPrompt(string? summaryOfPrevMessages, Guid agentId)
		{
			var language = GetCurrentLanguageMetadata();
			var template = templates.TryRetrieveBestWithFallback("system_prompt", language) as ITextTemplate;
			var promptSettings = agentSettings.GetAgentDescriptor(agentId).Prompts;

			var componentsContext = new
			{
			};

			var context = new
			{
				prompt = promptSettings.SystemPrompt,
				components = promptSettings.PromptComponents
					.Select(id => PromptRegistry.GetComponent(id)?.Template.Template.Render(componentsContext))
					.Where(c => !string.IsNullOrWhiteSpace(c))
					.ToArray(),
				assistantNickname = promptSettings.Nickname,
				persona = promptSettings.UseCustomPersona ?
					promptSettings.CustomPersona :
					(promptSettings.PersonaId != null ? PromptRegistry.GetPersona(promptSettings.PersonaId.Value)?.Template.Template.Render(componentsContext) : null),
				summary = string.IsNullOrWhiteSpace(summaryOfPrevMessages) ? null : summaryOfPrevMessages
			};

			return template!.Render(context);
		}

		private string BuildUserMessage(Domain.UserMessage message)
		{
			var language = GetCurrentLanguageMetadata();
			var template = templates.TryRetrieveBestWithFallback("user_message_prompt", language) as ITextTemplate;
			var context = new
			{
				time_sent = message.CreatedAt.ToString(),
				attachments = message.Attachments,
				content = message.Content
			};
			var result = template!.Render(context);
			return result;
		}

		/// <summary>
		/// Checks whether the agent with given <paramref name="agentId"/> can see a user message,
		/// based on visibility target and agent read permissions.
		/// </summary>
		private bool IsUserMessageVisibleToAgent(Domain.UserMessage userMessage, Guid agentId, AgentReadSettings readSettings)
		{
			var permissions = readSettings.ReadPermissions;

			if (!permissions.HasFlag(AgentReadPermissions.UserMessages))
				return false;

			if (permissions.HasFlag(AgentReadPermissions.UserAttachments))
				return true;

			return true;
		}

		/// <summary>
		/// Checks whether the agent with given <paramref name="agentId"/> can see an assistant message.
		/// </summary>
		private bool IsAssistantMessageVisibleToAgent(Domain.AssistantMessage assistantMessage, Guid agentId, AgentReadSettings readSettings)
		{
			var messageAgentId = assistantMessage.SenderAgent;
			var permissions = readSettings.ReadPermissions;

			// Own messages
			if (messageAgentId == agentId && permissions.HasFlag(AgentReadPermissions.OwnMessages))
				return true;

			// Other agent messages
			if (messageAgentId != agentId)
			{
				if (!permissions.HasFlag(AgentReadPermissions.OtherAgentMessages))
					return false;

				// Apply agent ID filter (white/black list)
				var filter = readSettings.AgentIdsReadFilter;
				if (filter.Count > 0)
				{
					bool inFilter = filter.Contains(messageAgentId);
					if (readSettings.IsFilterWhiteList && !inFilter)
						return false;
					if (!readSettings.IsFilterWhiteList && inFilter)
						return false;
				}

				return true;
			}

			return false;
		}

		/// <summary>
		/// Builds a merged "user message"-like representation of a foreign assistant's message,
		/// so the current agent sees what the other agent said as a quoted user message.
		/// Reasoning and tool calls are optionally stripped based on permissions.
		/// </summary>
		private string BuildForeignAgentMessageText(Domain.AssistantMessage assistantMessage, Guid currentAgentId, AgentReadSettings readSettings)
		{
			var agentDescriptor = agentSettings.GetAgentDescriptor(assistantMessage.SenderAgent);
			var agentName = agentDescriptor.Prompts.Nickname ?? agentDescriptor.Id.ToString()[..8];
			var permissions = readSettings.ReadPermissions;

			var sb = new StringBuilder();

			// Include reasoning if allowed
			if (permissions.HasFlag(AgentReadPermissions.OtherAgentReasoning) && !string.IsNullOrWhiteSpace(assistantMessage.ReasoningContent))
			{
				sb.AppendLine($"[{agentName} reasoning]:");
				sb.AppendLine(assistantMessage.ReasoningContent);
				sb.AppendLine();
			}

			sb.AppendLine($"[{agentName}]:");
			sb.AppendLine(assistantMessage.Content ?? "(no content)");

			// Include tool calls if allowed
			if (permissions.HasFlag(AgentReadPermissions.OtherAgentToolCalls) && assistantMessage.ToolCalls.Count > 0)
			{
				sb.AppendLine();
				sb.AppendLine($"[{agentName} tool calls]:");
				foreach (var toolCall in assistantMessage.ToolCalls)
				{
					sb.AppendLine($"  - {toolCall.ToolName}: {toolCall.ResultContent ?? "(no result)"}");
				}
			}

			return sb.ToString();
		}

		public IEnumerable<IMessage> ConvertMessage(ChatMessage message, Guid agentId)
		{
			var readSettings = agentSettings.GetAgentDescriptor(agentId).Read;

			if (message is Domain.UserMessage userMessage)
			{
				if (!IsUserMessageVisibleToAgent(userMessage, agentId, readSettings))
					return [];

				return BuildUserMessageAsMessages(userMessage);
			}
			else if (message is Domain.AssistantMessage assistantMessage)
			{
				if (!IsAssistantMessageVisibleToAgent(assistantMessage, agentId, readSettings))
					return [];

				// Own assistant message — full fidelity with tool calls
				if (assistantMessage.SenderAgent == agentId)
					return BuildOwnAssistantMessageAsMessages(assistantMessage);

				// Foreign assistant message — merged as quoted user message
				return BuildForeignAssistantMessageAsMessages(assistantMessage, agentId, readSettings);
			}
			else
			{
				throw new InvalidOperationException($"Unsupported message type: {message.GetType()}.");
			}
		}

		private IEnumerable<IMessage> BuildUserMessageAsMessages(Domain.UserMessage userMessage)
		{
			var resultMessage = new RCLargeLanguageModels.Messages.UserMessage(
				BuildUserMessage(userMessage)
			);
			return [resultMessage];
		}

		private IEnumerable<IMessage> BuildOwnAssistantMessageAsMessages(Domain.AssistantMessage assistantMessage)
		{
			List<IToolCall> toolCalls = [];
			List<IMessage> messages = [];

			foreach (var toolCall in assistantMessage.ToolCalls)
			{
				toolCalls.Add(new FunctionToolCall(toolCall.Id, toolCall.ToolName, toolCall.Arguments));
				var status = ConvertToolStatus(toolCall.Status);
				var toolResult = new ToolResult(status, toolCall.ResultContent ?? "Tool did returned no result.");
				messages.Add(new ToolMessage(toolResult, toolCall.Id, toolCall.ToolName));
			}

			var result = new RCLargeLanguageModels.Messages.AssistantMessage(assistantMessage.Content ?? "",
				assistantMessage.ReasoningContent, toolCalls: toolCalls);
			messages.Insert(0, result);

			return messages;
		}

		private IEnumerable<IMessage> BuildForeignAssistantMessageAsMessages(Domain.AssistantMessage assistantMessage, Guid agentId, AgentReadSettings readSettings)
		{
			var text = BuildForeignAgentMessageText(assistantMessage, agentId, readSettings);
			return [new RCLargeLanguageModels.Messages.UserMessage(text)];
		}

		/// <summary>
		/// Groups messages into rounds.
		/// <summary>
		/// Converts a message to LLM messages without applying agent-specific visibility filters.
		/// Used for summarization and other background processes.
		/// </summary>
		public IEnumerable<IMessage> ConvertMessageUnsafe(ChatMessage message)
		{
			if (message is Domain.UserMessage userMessage)
			{
				return BuildUserMessageAsMessages(userMessage);
			}
			else if (message is Domain.AssistantMessage assistantMessage)
			{
				return BuildOwnAssistantMessageAsMessages(assistantMessage);
			}
			else
			{
				throw new InvalidOperationException($"Unsupported message type: {message.GetType()}.");
			}
		}


		/// A round = [one or more consecutive user messages] + [one or more consecutive assistant messages].
		/// </summary>
		private static List<List<BranchedMessage>> GroupMessagesIntoRounds(IReadOnlyList<BranchedMessage> messages)
		{
			var rounds = new List<List<BranchedMessage>>();
			if (messages.Count == 0)
				return rounds;

			List<BranchedMessage>? currentRound = null;
			bool? lastWasUser = null;

			foreach (var branched in messages)
			{
				bool isUser = branched.Message is Domain.UserMessage;
				bool isAssistant = branched.Message is Domain.AssistantMessage;

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

		public IEnumerable<IMessage> Build(Guid agentId)
		{
			var readSettings = agentSettings.GetAgentDescriptor(agentId).Read;
			int maxRounds = chat.Settings.Environment.MaxVisibleRounds;

			// Build a flat list of visible messages first (filtered by permissions)
			var visibleMessages = new List<(int OriginalIndex, BranchedMessage Branched)>();
			for (int i = 0; i < chat.Messages.Count; i++)
			{
				var branched = chat.Messages[i];
				if (branched.Message is Domain.UserMessage userMsg)
				{
					if (IsUserMessageVisibleToAgent(userMsg, agentId, readSettings))
						visibleMessages.Add((i, branched));
				}
				else if (branched.Message is Domain.AssistantMessage asstMsg)
				{
					if (IsAssistantMessageVisibleToAgent(asstMsg, agentId, readSettings))
						visibleMessages.Add((i, branched));
				}
			}

			// Group into rounds
			var allRounds = GroupMessagesIntoRounds(visibleMessages.Select(v => v.Branched).ToList());

			// Apply round limit
			List<BranchedMessage> messagesToProcess;
			if (maxRounds > 0 && allRounds.Count > maxRounds)
			{
				messagesToProcess = allRounds
					.Skip(allRounds.Count - maxRounds)
					.SelectMany(r => r)
					.ToList();
			}
			else
			{
				messagesToProcess = visibleMessages.Select(v => v.Branched).ToList();
			}

			// Build the actual LLM message list
			List<IMessage> result = [];

			string? summaryOfPrevMessages = null;
			bool encounteredUserMessage = false;

			for (int i = messagesToProcess.Count - 1; i >= 0; i--)
			{
				var message = messagesToProcess[i].Message;

				if (message is Domain.UserMessage)
				{
					encounteredUserMessage = true;
					if (summaryOfPrevMessages != null)
					{
						result.InsertRange(0, ConvertMessage(message, agentId));
						break;
					}
				}

				if (!string.IsNullOrWhiteSpace(message.SummaryOfPrevMessages))
				{
					summaryOfPrevMessages = message.SummaryOfPrevMessages;
					if (encounteredUserMessage)
						break;
				}

				if (summaryOfPrevMessages == null)
				{
					result.InsertRange(0, ConvertMessage(message, agentId));
				}
			}

			string systemPrompt = BuildSystemPrompt(summaryOfPrevMessages, agentId);
			result.Insert(0, new SystemMessage(systemPrompt));

			return result;
		}
	}
}
