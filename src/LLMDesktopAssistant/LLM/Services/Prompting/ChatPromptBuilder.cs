using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM.Additional.Context;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Prompting.ContextExpanders;
using LLMDesktopAssistant.Prompting.Hooks;
using LLMDesktopAssistant.Prompting.Plugins;
using LLMDesktopAssistant.Prompting.Skills;
using LLMDesktopAssistant.Users;
using LLTSharp;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Messages.Attachments;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <summary>
	/// Builds the prompt context for a given agent, respecting:
	/// - <see cref="AgentReadSettings.ReadPermissions"/> — what the agent can see
	/// - <see cref="AgentReadSettings.AgentIdsReadFilter"/> — white/black list for other agents
	/// - <see cref="AgentReadSettings.Context"/> — how many recent rounds to include
	/// - Foreign agent messages are merged and presented as a single user message.
	/// </summary>
	[ChatService(typeof(IChatPromptBuilder))]
	public class ChatPromptBuilder(
		Chat chat,
		IChatSettingsService chatSettings,
		TemplateLibraryAccessor templates,
		IPromptRegistry promptRegistry,
		IAgentManagementService agentManager,
		IUserManagementService userManager,
		ISkillsetBuildingService skillsetBuilder,
		ISubAgentSetBuildingService subAgentSetBuilder,
		IEnumerable<IPromptBuildingHook> promptBuildingHooks,
		IEnumerable<IPromptSystemContextExpander> promptSystemContextExpanders,
		IEnumerable<IPromptMessageContextExpander> promptMessageContextExpanders,
		IEnumerable<IPromptTemplatePlugin> promptTemplatePlugins
		) : IChatPromptBuilder
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

		public string RenderSystemPrompt(ChatAgentDescriptor agent)
		{
			return BuildSystemPrompt(agent, GetTemplateFunctions());
		}

		private string BuildSystemPrompt(ChatAgentDescriptor agent, TemplateFunctionSet functions)
		{
			var template = templates.GetTextTemplate("system_prompt");
			var promptSettings = agent.Prompts;

			var generalContext = new Dictionary<string, object?>();
			foreach (var expander in promptSystemContextExpanders)
				expander.ExpandPromptContext(generalContext);
			var componentsContext = generalContext.ToDictionary(); // Clone

			var effectiveSystemPrompt = promptSettings.GetEffectiveSystemPrompt(chatSettings.Settings);
			var effectiveComponents = promptSettings.GetEffectivePromptComponents(chatSettings.Settings);
			var effectiveSliderValues = promptSettings.GetEffectiveSliderValues(chatSettings.Settings);
			var effectivePersona = promptSettings.GetEffectivePersona(chatSettings.Settings);
			var effectiveSpecialization = promptSettings.GetEffectiveSpecialization(chatSettings.Settings);
			var effectiveChatMemoryOptions = chatSettings.Settings.Memory.GetEffectiveMemoryOptions();

			generalContext["prompt"] = effectiveSystemPrompt;
			generalContext["components"] = effectiveComponents
				.Select(id => promptRegistry.GetComponent(id)?.Template.Template.Render(componentsContext))
				.Where(c => !string.IsNullOrWhiteSpace(c))
				.ToArray();
			generalContext["sliders"] = effectiveSliderValues.Select(s =>
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
			generalContext["assistant_nickname"] = effectivePersona.Nickname;
			generalContext["specialization"] = effectiveSpecialization.UseCustomSpecialization ?
				effectiveSpecialization.CustomSpecialization :
				(effectiveSpecialization.SpecializationId != null ? promptRegistry.GetSpecialization(effectiveSpecialization.SpecializationId.Value)?.Template.Template.Render(componentsContext) : null);
			generalContext["persona"] = effectivePersona.UseCustomPersona ?
				effectivePersona.CustomPersona :
				(effectivePersona.PersonaId != null ? promptRegistry.GetPersona(effectivePersona.PersonaId.Value)?.Template.Template.Render(componentsContext) : null);
			generalContext["skills"] = skillsetBuilder.GetSkillsForAgent(agent).Select(s => new
			{
				name = s.Name,
				description = s.Description,
				path = s.Path,
				body = s.InjectionMode is SkillInjectionMode.Full ? s.BodyGetter() : null
			});
			generalContext["sub_agents"] = subAgentSetBuilder.GetSubAgentsForAgent(agent).Select(s => new
			{
				name = s.Name,
				description = s.Description
			});
			generalContext["memory_blocks"] = effectiveChatMemoryOptions.EnableMemory && effectiveChatMemoryOptions.ManualControlEnabled && agent.Memory.EnableMemory
				? agent.Memory.GetEnabledBlocks(chatSettings.Settings)
					.Select(b => new
					{
						name = b.Block.Name,
						can_read = b.Attachment.AllowsReading(),
						can_write = b.Attachment.AllowsWriting(),
						facts_enabled = b.Block.FactsEnabled,
						logs_enabled = b.Block.LogsEnabled,
						description = b.Block.Description
					})
				: null;

			return template!.Render(generalContext, functions);
		}

		private RCLargeLanguageModels.Messages.UserMessage BuildUserMessage(BranchedMessage message,
			TemplateFunctionSet functions)
		{
			var userMessage = message.AsUserMessage();
			var template = templates.GetTextTemplate("user_message_prompt");

			var context = new Dictionary<string, object?>();
			foreach (var expander in promptSystemContextExpanders)
				expander.ExpandPromptContext(context);
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
			ChatAgentDescriptor agent, TemplateFunctionSet functions)
		{
			var userMessage = message.AsUserMessage();
			var template = templates.GetTextTemplate("user_message_prompt");

			var context = new Dictionary<string, object?>();
			foreach (var expander in promptMessageContextExpanders)
				expander.ExpandPromptContext(message, agent, context);

			string userName = userManager.FindByLogin(userMessage.SenderLogin)?.GetAgentShownName() ?? userMessage.SenderLogin;
			context["user_name"] = userName;
			context["time_sent"] = userMessage.CreatedAt.ToString();
			context["content"] = userMessage.Content;
			context["attachments"] = userMessage.Attachments;
			context["can_read_content"] = true;
			bool canReadAttachments = agent.Read.GetEffectiveReadPermissions(chatSettings.Settings).HasFlag(AgentReadPermissions.UserAttachments);
			context["can_read_attachments"] = canReadAttachments;

			var result = template!.Render(context, functions);
			IEnumerable<IAttachment> attachments = [];
			if (canReadAttachments)
				attachments = userMessage.Attachments.Select(a => a.NativeAttachment).Where(a => a != null)!;
			return new RCLargeLanguageModels.Messages.UserMessage(userName, result, attachments);
		}

		private RCLargeLanguageModels.Messages.UserMessage BuildForeignAgentMessageText(BranchedMessage message,
			ChatAgentDescriptor agent, TemplateFunctionSet functions)
		{
			var assistantMessage = message.AsAssistantMessage();
			var senderDescriptor = agentManager.GetAgentDescriptor(assistantMessage.SenderAgentId);
			var agentName = senderDescriptor.Info.Name ?? senderDescriptor.Id.ToString()[..8];
			var exposure = senderDescriptor.Read.GetEffectiveExposureMode(chatSettings.Settings); // What sender agent exposes
			var permissions = agent.Read.GetEffectiveReadPermissions(chatSettings.Settings); // What current agent can see

			if (assistantMessage.IsUserLike || permissions.HasFlag(AgentReadPermissions.IdentifyAgentsAsUsers))
			{
				var template = templates.GetTextTemplate("user_message_prompt");

				var context = new Dictionary<string, object?>();
				foreach (var expander in promptMessageContextExpanders)
					expander.ExpandPromptContext(message, agent, context);

				context["user_name"] = agentName;
				context["time_sent"] = assistantMessage.CreatedAt.ToString();
				context["content"] = assistantMessage.Content;
				context["attachments"] = assistantMessage.Attachments;
				// User-like messages are already gated by user read permissions and their content is always readable
				context["can_read_content"] = assistantMessage.IsUserLike ||
					(permissions.HasFlag(AgentReadPermissions.OtherAgentContent) &&
					exposure.HasFlag(AgentExposureMode.Content));
				bool canReadAttachments = assistantMessage.IsUserLike
					? permissions.HasFlag(AgentReadPermissions.UserAttachments)
					: permissions.HasFlag(AgentReadPermissions.OtherAgentAttachments) && exposure.HasFlag(AgentExposureMode.Attachments);
				context["can_read_attachments"] = canReadAttachments;

				var result = template!.Render(context, functions);
				IEnumerable<IAttachment> attachments = [];
				if (canReadAttachments)
					attachments = assistantMessage.Attachments.Select(a => a.NativeAttachment).Where(a => a != null)!;
				return new RCLargeLanguageModels.Messages.UserMessage(agentName, result, attachments);
			}
			else
			{
				var template = templates.GetTextTemplate("foreign_assistant_prompt");

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
			ChatAgentDescriptor agent, TemplateFunctionSet functions)
		{
			if (message.Message is Domain.UserMessage)
			{
				if (!AgentMessageVisibility.IsUserMessageVisibleToAgent(message, agent, chatSettings.Settings))
					return [];

				return [BuildUserMessageForAgent(message, agent, functions)];
			}
			else if (message.Message is Domain.AssistantMessage assistantMessage)
			{
				if (!AgentMessageVisibility.IsAssistantMessageVisibleToAgent(message, agent, agentManager, chatSettings.Settings))
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
				var previewAgent = new ChatAgentDescriptor();
				previewAgent.Read.SetEffectiveReadPermissions(chatSettings.Settings, (AgentReadPermissions)0x7fffffff);
				return BuildForeignAgentMessageText(assistantMessage, previewAgent, functions).Content;
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

		public IEnumerable<IMessage> Build(ChatAgentDescriptor agent)
		{
			var readContext = agent.Read.GetEffectiveContext(chatSettings.Settings);
			int maxRounds = readContext.MaxVisibleRounds;

			var hooks = promptBuildingHooks.OrderBy(h => h.Order).ToList();

			var functions = GetTemplateFunctions();

			var messagesToProcess = MessagesInterface
				.GroupMessagesIntoRounds(chat.Messages, maxRounds)
				.SelectMany(g => g)
				.ToList();

			List<IMessage> result = [];

			string? summaryOfPrevMessages = null;
			bool encounteredUserMessage = false;

			for (int i = messagesToProcess.Count - 1; i >= 0; i--)
			{
				var branchedMessage = messagesToProcess[i];
				var message = branchedMessage.Message;

				if (readContext.AllowContextShields && message.AdditionalViewModels.Has<ContextShieldViewModel>())
				{
					break;
				}
				if (message is Domain.UserMessage)
				{
					if (!AgentMessageVisibility.IsUserMessageVisibleToAgent(branchedMessage, agent, chatSettings.Settings))
						continue;
				}
				else if (message is Domain.AssistantMessage assistantMessage)
				{
					if (assistantMessage.IsCompleted && !AgentMessageVisibility.IsAssistantMessageVisibleToAgent(branchedMessage, agent, agentManager, chatSettings.Settings))
						continue;
				}

				if (message is Domain.UserMessage || message is Domain.AssistantMessage { IsUserLike: true })
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

				if (readContext.AllowSummaries &&
					message.AdditionalViewModels.TryGet<SummaryViewModel>(out var summaryViewModel) &&
					summaryViewModel.Completed)
				{
					summaryOfPrevMessages = summaryViewModel.Summary;
					if (encounteredUserMessage)
						break;
				}

				if (summaryOfPrevMessages == null)
				{
					IEnumerable<IMessage> messages;
					if (message is Domain.AssistantMessage assistantMessage && !assistantMessage.IsCompleted)
						messages = [];
					else
						messages = ConvertMessageForAgent(branchedMessage, agent, functions);
					foreach (var hook in hooks)
					{
						var editedMessages = hook.ModifyFinalContext(messages, branchedMessage, agent);
						if (editedMessages != null)
							messages = editedMessages;
					}
					result.InsertRange(0, messages);
				}
			}

			string systemPrompt = BuildSystemPrompt(agent, functions);
			if (summaryOfPrevMessages != null)
				result.Insert(0, new RCLargeLanguageModels.Messages.UserMessage(Senders.User, $"""
					<summary>
					{summaryOfPrevMessages}
					</summary>
					"""));
			result.Insert(0, new SystemMessage(systemPrompt));

			return result;
		}
	}
}
