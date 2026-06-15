using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM.Additional.Context;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Prompting.ContextExpanders;
using LLMDesktopAssistant.Prompting.Hooks;
using LLMDesktopAssistant.Prompting.Injectors;
using LLMDesktopAssistant.Prompting.Plugins;
using LLMDesktopAssistant.WebUI;
using LLTSharp;
using LLTSharp.DataAccessors;
using LLTSharp.Locale;
using LLTSharp.Metadata;
using LLTSharp.Metadata.Types;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Messages.Attachments;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;
using Serilog;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// Builds the prompt context for a given agent, respecting:
	/// - <see cref="AgentReadSettings.ReadPermissions"/> — what the agent can see
	/// - <see cref="AgentReadSettings.AgentIdsReadFilter"/> — white/black list for other agents
	/// - <see cref="AgentReadSettings.MaxVisibleRounds"/> — how many recent rounds to include
	/// - Foreign agent messages are merged and presented as a single user message.
	/// </summary>
	[ChatService(typeof(IPromptChatBuilder))]
	public class PromptChatBuilder(
		Chat chat,
		TemplateLibrary templates,
		IPromptRegistry promptRegistry,
		IAgentManagementService agentManager,
		IUserManagementService userManager,
		IEnumerable<IPromptInjector> promptInjectors,
		IEnumerable<IPromptBuildingHook> promptBuildingHooks,
		IEnumerable<IPromptSystemContextExpander> promptSystemContextExpanders,
		IEnumerable<IPromptMessageContextExpander> promptMessageContextExpanders,
		IEnumerable<IPromptTemplatePlugin> promptTemplatePlugins
		) : IPromptChatBuilder
	{
		private TemplateFunctionSet GetTemplateFunctions()
		{
			return new(promptTemplatePlugins.SelectMany(p => p.GetTemplateFunctions()));
		}

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
				_ => ToolResultStatus.NoResult
			};
		}

		private LanguageMetadata GetCurrentLanguageMetadata()
		{
			return new LanguageMetadata(new LanguageCode(CultureInfo.CurrentCulture));
		}

		private bool IsUserMessageVisibleToAgent(BranchedMessage message, AgentDescriptor agent)
		{
			var userMessage = message.AsUserMessage();
			var permissions = agent.Read.ReadPermissions;

			if (!permissions.HasFlag(AgentReadPermissions.UserMessages))
				return false;

			switch (userMessage.Visibility)
			{
				case MessageVisibility.OnlyUsers:
					return false;
				case MessageVisibility.OnlyAgents:
				case MessageVisibility.Always:
				case MessageVisibility.RevealAfterSend:
				default:
					break;
			}

			// If its a white list, then 'contains' must return true to skip this check -> true == true
			// If its a black list, then 'contains' must return false to skip this check -> false == false
			if (userMessage.VisibleTo.Contains(agent.Id.ToString()) != userMessage.IsVisibleToWhiteList)
				return false;

			return true;
		}

		private bool IsAssistantMessageVisibleToAgent(BranchedMessage message, AgentDescriptor agent)
		{
			var assistantMessage = message.AsAssistantMessage();
			var messageAgentId = assistantMessage.SenderAgentId;
			var agentDescriptor = agentManager.GetAgentDescriptor(assistantMessage.SenderAgentId);
			var exposure = agentDescriptor.Read.ExposureMode; // What sender agent exposes
			var permissions = agent.Read.ReadPermissions; // What current agent can see

			// Own messages
			if (messageAgentId == agent.Id)
			{
				if (permissions.HasFlag(AgentReadPermissions.OwnMessages))
					return true;

				return false;
			}

			// Other agent messages
			if (!permissions.HasFlag(AgentReadPermissions.OtherAgentMessages))
				return false;

			// Messages with tool calls
			if (assistantMessage.ToolCalls.Count > 0 && !(permissions.HasFlag(AgentReadPermissions.MessagesWithToolCalls)
				&& exposure.HasFlag(AgentExposureMode.MessagesWithToolCalls)))
				return false;

			// Apply agent ID filter (white/black list)
			var filter = agent.Read.AgentIdsReadFilter;
			if (filter.Count > 0)
			{
				bool inFilter = filter.Contains(messageAgentId);
				if (agent.Read.IsFilterWhiteList && !inFilter)
					return false;
				if (!agent.Read.IsFilterWhiteList && inFilter)
					return false;
			}

			return true;
		}

		private string BuildSystemPrompt(string? summaryOfPrevMessages,
			AgentDescriptor agent, TemplateFunctionSet functions)
		{
			var language = GetCurrentLanguageMetadata();
			var template = templates.TryRetrieveBestAllWithFallback("system_prompt", language)?.LastOrDefault() as ITextTemplate;
			var promptSettings = agent.Prompts;

			var generalContext = new Dictionary<string, object?>();
			foreach (var expander in promptSystemContextExpanders)
				expander.ExpandPromptContext(generalContext);
			var componentsContext = generalContext.ToDictionary(); // Clone

			generalContext["prompt"] = promptSettings.SystemPrompt;
			generalContext["components"] = promptSettings.PromptComponents
				.Select(id => promptRegistry.GetComponent(id)?.Template.Template.Render(componentsContext))
				.Where(c => !string.IsNullOrWhiteSpace(c))
				.ToArray();
			generalContext["sliders"] = promptSettings.SliderValues.Select(s =>
				{
					var sliderTemplate = promptRegistry.GetSlider(s.SliderId)?.Template.Template;
					var sliderContext = new
					{
						sliderValue = s.Value
					};
					return sliderTemplate?.Render(sliderContext);
				})
				.Where(c => !string.IsNullOrWhiteSpace(c))
				.ToArray();
			generalContext["assistantNickname"] = promptSettings.Nickname;
			generalContext["specialization"] = promptSettings.UseCustomSpecialization ?
				promptSettings.CustomSpecialization :
				(promptSettings.SpecializationId != null ? promptRegistry.GetSpecialization(promptSettings.SpecializationId.Value)?.Template.Template.Render(componentsContext) : null);
			generalContext["persona"] = promptSettings.UseCustomPersona ?
				promptSettings.CustomPersona :
				(promptSettings.PersonaId != null ? promptRegistry.GetPersona(promptSettings.PersonaId.Value)?.Template.Template.Render(componentsContext) : null);
			generalContext["summary"] = string.IsNullOrWhiteSpace(summaryOfPrevMessages) ? null : summaryOfPrevMessages;

			return template!.Render(generalContext, functions);
		}

		private RCLargeLanguageModels.Messages.UserMessage BuildUserMessage(BranchedMessage message,
			TemplateFunctionSet functions)
		{
			var userMessage = message.AsUserMessage();
			var language = GetCurrentLanguageMetadata();
			var template = templates.TryRetrieveBestAllWithFallback("user_message_prompt", language)?.LastOrDefault() as ITextTemplate;

			var context = new Dictionary<string, object?>();
			foreach (var expander in promptMessageContextExpanders)
				expander.ExpandPromptContext(message, null, context);

			string userName = userManager.FindByLogin(userMessage.SenderLogin)?.GetAgentShownName() ?? userMessage.SenderLogin;
			context["user_name"] = userName;
			context["time_sent"] = userMessage.CreatedAt.ToString();
			context["content"] = userMessage.Content;
			context["attachments"] = userMessage.Attachments;
			context["can_read_content"] = true;
			context["can_read_attachments"] = true;

			var result = template!.Render(context, functions);
			var attachments = userMessage.Attachments.Select(a => a.NativeAttachment).Where(a => a != null)!;
			return new RCLargeLanguageModels.Messages.UserMessage(userName, result, attachments!);
		}

		private RCLargeLanguageModels.Messages.UserMessage BuildUserMessageForAgent(BranchedMessage message,
			AgentDescriptor agent, TemplateFunctionSet functions)
		{
			var userMessage = message.AsUserMessage();
			var language = GetCurrentLanguageMetadata();
			var template = templates.TryRetrieveBestAllWithFallback("user_message_prompt", language)?.LastOrDefault() as ITextTemplate;

			var context = new Dictionary<string, object?>();
			foreach (var expander in promptMessageContextExpanders)
				expander.ExpandPromptContext(message, agent, context);

			string userName = userManager.FindByLogin(userMessage.SenderLogin)?.GetAgentShownName() ?? userMessage.SenderLogin;
			context["user_name"] = userName;
			context["time_sent"] = userMessage.CreatedAt.ToString();
			context["content"] = userMessage.Content;
			context["attachments"] = userMessage.Attachments;
			context["can_read_content"] = true;
			bool canReadAttachments = agent.Read.ReadPermissions.HasFlag(AgentReadPermissions.UserAttachments);
			context["can_read_attachments"] = canReadAttachments;

			var result = template!.Render(context, functions);
			IEnumerable<IAttachment> attachments = [];
			if (canReadAttachments)
				attachments = userMessage.Attachments.Select(a => a.NativeAttachment).Where(a => a != null)!;
			return new RCLargeLanguageModels.Messages.UserMessage(userName, result, attachments);
		}

		private RCLargeLanguageModels.Messages.UserMessage BuildForeignAgentMessageText(BranchedMessage message,
			AgentDescriptor agent, TemplateFunctionSet functions)
		{
			var assistantMessage = message.AsAssistantMessage();
			var senderDescriptor = agentManager.GetAgentDescriptor(assistantMessage.SenderAgentId);
			var agentName = senderDescriptor.Info.Name ?? senderDescriptor.Id.ToString()[..8];
			var exposure = senderDescriptor.Read.ExposureMode; // What sender agent exposes
			var permissions = agent.Read.ReadPermissions; // What current agent can see

			var language = GetCurrentLanguageMetadata();
			if (exposure.HasFlag(AgentExposureMode.IdentifySelfAsUser) || permissions.HasFlag(AgentReadPermissions.IdentifyAgentsAsUsers))
			{
				var template = templates.TryRetrieveBestAllWithFallback("user_message_prompt", language)?.LastOrDefault() as ITextTemplate;

				var context = new Dictionary<string, object?>();
				foreach (var expander in promptMessageContextExpanders)
					expander.ExpandPromptContext(message, agent, context);

				context["user_name"] = agentName;
				context["time_sent"] = assistantMessage.CreatedAt.ToString();
				context["content"] = assistantMessage.Content;
				context["attachments"] = assistantMessage.Attachments;
				context["can_read_content"] =
					permissions.HasFlag(AgentReadPermissions.OtherAgentContent) &&
					exposure.HasFlag(AgentExposureMode.Content);
				bool canReadAttachments =
					permissions.HasFlag(AgentReadPermissions.OtherAgentAttachments) &&
					exposure.HasFlag(AgentExposureMode.Attachments);
				context["can_read_attachments"] = canReadAttachments;

				var result = template!.Render(context, functions);
				IEnumerable<IAttachment> attachments = [];
				if (canReadAttachments)
					attachments = assistantMessage.Attachments.Select(a => a.NativeAttachment).Where(a => a != null)!;
				return new RCLargeLanguageModels.Messages.UserMessage(agentName, result, attachments);
			}
			else
			{
				var template = templates.TryRetrieveBestAllWithFallback("foreign_assistant_prompt", language)?.LastOrDefault() as ITextTemplate;

				var context = new Dictionary<string, object?>();
				foreach (var expander in promptMessageContextExpanders)
					expander.ExpandPromptContext(message, agent, context);

				context["agent_name"] = agentName;
				context["time_sent"] = assistantMessage.CreatedAt.ToString();
				context["reasoning_content"] = assistantMessage.ReasoningContent;
				context["content"] = assistantMessage.Content;
				context["attachments"] = assistantMessage.Attachments;
				context["tool_calls"] = assistantMessage.ToolCalls.Select(tc => new
					{
						name = tc.ToolName,
						arguments = tc.Arguments,
						result_content = tc.ResultContent,
					}).ToArray();

				context["can_read_reasoning"] =
					permissions.HasFlag(AgentReadPermissions.OtherAgentReasoning) &&
					exposure.HasFlag(AgentExposureMode.Reasoning);
				context["can_read_content"] =
					permissions.HasFlag(AgentReadPermissions.OtherAgentContent) &&
					exposure.HasFlag(AgentExposureMode.Content);
				bool canReadAttachments =
					permissions.HasFlag(AgentReadPermissions.OtherAgentAttachments) &&
					exposure.HasFlag(AgentExposureMode.Attachments);
				context["can_read_attachments"] = canReadAttachments;
				context["can_read_tool_calls"] =
					permissions.HasFlag(AgentReadPermissions.OtherAgentToolCalls) &&
					exposure.HasFlag(AgentExposureMode.ToolCalls);

				var result = template!.Render(context, functions);
				IEnumerable<IAttachment> attachments = [];
				if (canReadAttachments)
					attachments = assistantMessage.Attachments.Select(a => a.NativeAttachment).Where(a => a != null)!;
				return new RCLargeLanguageModels.Messages.UserMessage(agentName, result, attachments);
			}
		}

		private IEnumerable<IMessage> ConvertMessageForAgent(BranchedMessage message,
			AgentDescriptor agent, TemplateFunctionSet functions)
		{
			if (message.Message is Domain.UserMessage)
			{
				if (!IsUserMessageVisibleToAgent(message, agent))
					return [];

				return [BuildUserMessageForAgent(message, agent, functions)];
			}
			else if (message.Message is Domain.AssistantMessage assistantMessage)
			{
				if (!IsAssistantMessageVisibleToAgent(message, agent))
					return [];

				// Own assistant message — full fidelity with tool calls
				if (assistantMessage.SenderAgentId == agent.Id)
					return BuildOwnAssistantMessageAsMessages(assistantMessage);

				// Foreign assistant message — merged as quoted user message
				return [BuildForeignAgentMessageText(message, agent, functions)];
			}
			else if (message.Message is RawUserMessage rawUserMessage)
			{
				var attachments = rawUserMessage.Attachments.Select(a => a.NativeAttachment).Where(a => a != null);
				var userMessage = new RCLargeLanguageModels.Messages.UserMessage(Senders.User, rawUserMessage.Content, attachments!);
				return [userMessage];
			}
			else
			{
				throw new InvalidOperationException($"Unsupported message type: {message.GetType()}.");
			}
		}

		public string RenderMessage(BranchedMessage message)
		{
			var functions = GetTemplateFunctions();

			if (message.Message is Domain.UserMessage userMessage)
			{
				return BuildUserMessage(userMessage, functions).Content;
			}

			if (message.Message is Domain.AssistantMessage assistantMessage)
			{
				return BuildForeignAgentMessageText(assistantMessage, new AgentDescriptor
				{
					Read = new AgentReadSettings
					{
						ReadPermissions = (AgentReadPermissions)0x7fffffff
					}
				}, functions).Content;
			}

			throw new InvalidOperationException($"Unsupported message type: {message.GetType()}.");
		}

		private IEnumerable<IMessage> BuildOwnAssistantMessageAsMessages(Domain.AssistantMessage assistantMessage)
		{
			List<IToolCall> toolCalls = [];
			List<IMessage> messages = [];

			foreach (var toolCall in assistantMessage.ToolCalls)
			{
				toolCalls.Add(new FunctionToolCall(toolCall.Id, toolCall.ToolName, toolCall.Arguments ?? string.Empty));
				var status = ConvertToolStatus(toolCall.Status);
				var resultContent = toolCall.ResultContent ?? string.Empty;
				var toolResult = new ToolResult(status, resultContent,
					toolCall.Attachments.Select(a => a.NativeAttachment).Where(a => a != null)!);
				messages.Add(new ToolMessage(toolResult, toolCall.Id, toolCall.ToolName));
			}

			var result = new RCLargeLanguageModels.Messages.AssistantMessage(
				assistantMessage.Content ?? "",
				assistantMessage.ReasoningContent ?? "",
				toolCalls: toolCalls,
				attachments: assistantMessage.Attachments.Select(a => a.NativeAttachment).Where(a => a != null)!);
			messages.Insert(0, result);

			return messages;
		}

		/// <summary>
		/// Converts a message to LLM messages without applying agent-specific visibility filters.
		/// Used for summarization and other background processes.
		/// </summary>
		public IEnumerable<IMessage> ConvertMessage(BranchedMessage message)
		{
			var functions = GetTemplateFunctions();

			if (message.Message is Domain.UserMessage userMessage)
			{
				return [BuildUserMessage(userMessage, functions)];
			}
			else if (message.Message is Domain.AssistantMessage assistantMessage)
			{
				return BuildOwnAssistantMessageAsMessages(assistantMessage);
			}
			else
			{
				throw new InvalidOperationException($"Unsupported message type: {message.GetType()}.");
			}
		}

		public IEnumerable<IMessage> Build(AgentDescriptor agent)
		{
			var readSettings = agent.Read;
			int maxRounds = readSettings.MaxVisibleRounds;

			var injectors = promptInjectors.OrderBy(i => i.Order).ToList();
			var hooks = promptBuildingHooks.OrderBy(h => h.Order).ToList();

			var functions = GetTemplateFunctions();

			var messagesToProcess = MessagesInterface
				.GroupMessagesIntoRounds(chat.Messages, maxRounds)
				.SelectMany(g => g)
				.ToList();

			foreach (var injector in injectors)
				injector.Inject(messagesToProcess, agent);

			List<IMessage> result = [];

			string? summaryOfPrevMessages = null;
			bool encounteredUserMessage = false;

			for (int i = messagesToProcess.Count - 1; i >= 0; i--)
			{
				var branchedMessage = messagesToProcess[i];

				foreach (var hook in hooks)
				{
					branchedMessage = hook.Modify(branchedMessage, agent);
					if (branchedMessage == null)
						break;
				}
				if (branchedMessage == null)
					continue;
				var message = branchedMessage.Message;

				if (readSettings.AllowContextShields && message.AdditionalViewModels.Has<ContextShieldViewModel>())
				{
					break;
				}
				if (message is Domain.UserMessage)
				{
					if (!IsUserMessageVisibleToAgent(branchedMessage, agent))
						continue;
				}
				else if (message is Domain.AssistantMessage)
				{
					if (!IsAssistantMessageVisibleToAgent(branchedMessage, agent))
						continue;
				}

				if (message is Domain.UserMessage)
				{
					encounteredUserMessage = true;
					if (summaryOfPrevMessages != null)
					{
						var messages = ConvertMessageForAgent(branchedMessage, agent, functions);
						foreach (var hook in hooks)
						{
							var editedMessages = hook.ModifyFinalContext(messages, branchedMessage, agent);
							if (editedMessages != null)
								messages = editedMessages;
						}
						result.InsertRange(0, messages);
						break;
					}
				}

				if (readSettings.AllowSummaries &&
					message.AdditionalViewModels.TryGet<SummaryViewModel>(out var summaryViewModel) &&
					summaryViewModel.Completed)
				{
					summaryOfPrevMessages = summaryViewModel.Summary;
					if (encounteredUserMessage)
						break;
				}

				if (summaryOfPrevMessages == null)
				{
					var messages = ConvertMessageForAgent(branchedMessage, agent, functions);
					foreach (var hook in hooks)
					{
						var editedMessages = hook.ModifyFinalContext(messages, branchedMessage, agent);
						if (editedMessages != null)
							messages = editedMessages;
					}
					result.InsertRange(0, messages);
				}
			}

			string systemPrompt = BuildSystemPrompt(summaryOfPrevMessages, agent, functions);
			result.Insert(0, new SystemMessage(systemPrompt));

			/*var array = new JsonArray();
			foreach (var message in result)
			{
				switch (message)
				{
					case SystemMessage system:
						array.Add(new JsonObject
						{
							["type"] = "system",
							["content"] = system.Content
						});
						break;
					case RCLargeLanguageModels.Messages.UserMessage user:
						array.Add(new JsonObject
						{
							["type"] = "user",
							["content"] = user.Content
						});
						break;
					case RCLargeLanguageModels.Messages.AssistantMessage assistant:
						array.Add(new JsonObject
						{
							["type"] = "assistant",
							["reasoning_content"] = assistant.ReasoningContent,
							["content"] = assistant.Content,
							["tool_calls"] = new JsonArray(assistant.ToolCalls.Select(tc => new JsonObject
							{
								["type"] = "function",
								["id"] = tc.Id,
								["name"] = tc.ToolName,
								["arguments"] = ((FunctionToolCall)tc).Args.DeepClone()
							}).ToArray())
						});
						break;
					case ToolMessage tool:
						array.Add(new JsonObject
						{
							["type"] = "tool",
							["id"] = tool.ToolCallId,
							["name"] = tool.ToolName,
							["content"] = tool.Content
						});
						break;
				}
			}
			File.WriteAllText($"{DateTime.Now:yyyyMMddHHmmssfff}.json", array.ToJsonString(new System.Text.Json.JsonSerializerOptions
			{
				WriteIndented = true,
				Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
			}));*/

			return result;
		}
	}
}
