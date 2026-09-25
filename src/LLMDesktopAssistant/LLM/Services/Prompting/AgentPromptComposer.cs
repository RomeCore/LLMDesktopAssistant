using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.Settings;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM.Additional;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Prompting.ContextExpanders;
using LLMDesktopAssistant.Prompting.Hooks;
using LLMDesktopAssistant.Prompting.Plugins;
using LLMDesktopAssistant.Prompting.Context;
using LLMDesktopAssistant.Users;
using LLTSharp;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Messages.Attachments;
using RCLargeLanguageModels.Tools;
using Serilog;
using LLMDesktopAssistant.Addons;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <inheritdoc cref="IAgentPromptComposer"/>
	[ChatService(typeof(IAgentPromptComposer))]
	public class AgentPromptComposer(
		IChatSettingsService chatSettings,
		ITemplateLibraryAccessor templates,
		IAgentManagementService agentManager,
		IMessageVisibilityService messageVisibility,
		IAgentEffectiveMessagesProvider effectiveMessagesProvider,
		IUserManagementService userManager,
		IEnumerable<IPromptContextProvider> promptSections,
		IEnumerable<IPromptBuildingHook> promptBuildingHooks,
		IEnumerable<IPromptMessageContextExpander> promptMessageContextExpanders,
		IEnumerable<IPromptTemplatePlugin> promptTemplatePlugins,
		IToolsetCacheService toolsetCache,
		IPromptSectionProcessor promptSectionProcessor,
		IAddonSetCollector<PromptContextInfo> promptContextCollector,
		IPromptDumpService promptDumpService
		) : IAgentPromptComposer
	{
		/// <inheritdoc/>
		public AgentPromptBundle Build(ChatAgentDescriptor agent)
		{
			// The composer owns the toolset cache: it must be fresh for both the tool definitions
			// and the tool call resolution performed by the execution service.
			toolsetCache.Invalidate(agent);

			var effective = effectiveMessagesProvider.GetEffectiveMessages(agent);

			var hooks = promptBuildingHooks.OrderBy(h => h.Order).ToList();
			var functions = new TemplateFunctionSet(promptTemplatePlugins.SelectMany(p => p.GetTemplateFunctions()));

			List<IMessage> result = [];
			var disabledCheckpoints = agent.Context.GetEffectiveDisabledFlags(chatSettings.Settings);

			var promptContextInfos = promptContextCollector.GetAddonsForAgent(agent);
			var promptContextProviders = promptContextInfos.Select(i => i.Provider).ToArray();

			var promptMode = agent.Context.PromptMode;
			var anchor = promptSectionProcessor.Process(agent, effective, promptContextProviders);

			SystemPromptSnapshot header;
			string headerSource;
			switch (promptMode)
			{
				case PromptContextMode.Static:
					var settings = agent.Context;
					if (settings.Snapshot is null)
					{
						settings.Snapshot = promptSections.Anchored().RenderHeader(agent);
						Log.Information("Froze static system prompt snapshot for agent {AgentId}.", agent.Id);
					}

					header = settings.Snapshot;
					headerSource = "static";
					break;

				case PromptContextMode.Hybrid when anchor != null:
					header = anchor.Snapshot;
					headerSource = $"anchor#{anchor.Id}";
					break;

				default:
					header = promptContextProviders.Anchored().RenderHeader(agent);
					headerSource = "live";
					break;
			}

			Log.Debug("Prompt header for agent {AgentId}: mode={Mode}, source={Source}.", agent.Id, promptMode, headerSource);

			var summaryCheckpoint = effective.Checkpoints.LastOrDefault(c =>
				(c.Checkpoint.Kind & ~disabledCheckpoints).HasFlag(ContextCheckpointKind.Summary));

			result.Add(new SystemMessage(header.Text));
			if (summaryCheckpoint != null)
				result.Add(new RCLargeLanguageModels.Messages.UserMessage(Senders.User, $"""
					<summary>
					{summaryCheckpoint.Checkpoint.Context}
					</summary>
					"""));

			for (int i = 0; i < effective.Messages.Count; i++)
			{
				var branchedMessage = effective.Messages[i];
				var compaction = MessageCompaction.ForMessage(effective.Checkpoints, i, disabledCheckpoints);

				IEnumerable<IMessage> messages;
				if (branchedMessage.Message is Domain.AssistantMessage assistantMessage && !assistantMessage.IsCompleted)
					messages = [];
				else
					messages = ConvertMessageForAgent(branchedMessage, agent, functions, compaction);

				foreach (var hook in hooks)
				{
					var editedMessages = hook.ModifyFinalContext(messages, branchedMessage, agent);
					if (editedMessages != null)
						messages = editedMessages;
				}

				// Process SCM deltas.
				if (anchor is not null && branchedMessage.Message is Domain.AssistantMessage)
				{
					foreach (var delta in branchedMessage.Message.AdditionalData.OfType<PromptStateDeltaMessageData>())
					{
						if (delta.AnchorId != anchor.Id)
							continue;

						result.Add(new RCLargeLanguageModels.Messages.UserMessage("system", delta.Snapshot));
					}
				}

				result.AddRange(messages);
			}

			List<FunctionTool> tools;
			if (promptMode == PromptContextMode.Dynamic)
			{
				tools = toolsetCache.ValidTools.Values
					.Where(t => !(t.Hidden ?? false))
					.Select(t => t.NativeTool)
					.OrderBy(t => t.Name)
					.ToList();
			}
			else
			{
				tools = header.Tools
					.Select(t => t.ToFunctionTool())
					.ToList();
			}

			if (PromptDumpService.IsEnabled)
				promptDumpService.Dump(result, tools, $"mode={promptMode}; header={headerSource}");

			return new AgentPromptBundle(result, tools);
		}

		/// <summary>
		/// Gets the attachment parts stored in the additional view models of a chat object (message or tool call).
		/// </summary>
		private static IEnumerable<AttachmentMessagePart> GetAttachmentParts(ChatObjectBase chatObject) =>
			chatObject.AdditionalData.GetAll<AttachmentMessagePart>();

		/// <summary>
		/// Gets the attachment parts stored in the additional view models of a chat object (message or tool call).
		/// </summary>
		private static IEnumerable<IAttachment> GetNativeAttachments(ChatObjectBase chatObject) =>
			chatObject.AdditionalData.GetAll<NativeAttachmentMessagePart>().Select(a => a.NativeAttachment!).Where(a => a != null);

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
			context["time_sent"] = FormatSentTime(userMessage.CreatedAt);
			context["content"] = userMessage.Content;
			context["attachments"] = GetAttachmentParts(userMessage);
			context["can_read_content"] = true;
			bool canReadAttachments = agent.Read.GetEffectiveReadPermissions(chatSettings.Settings).HasFlag(AgentReadPermissions.UserAttachments);
			context["can_read_attachments"] = canReadAttachments;

			var result = template.Render(context, functions);
			IEnumerable<IAttachment> attachments = [];
			if (canReadAttachments)
				attachments = GetNativeAttachments(userMessage);
			return new RCLargeLanguageModels.Messages.UserMessage(userName, result, attachments);
		}

		private RCLargeLanguageModels.Messages.UserMessage BuildForeignAgentMessageText(BranchedMessage message,
			ChatAgentDescriptor agent, TemplateFunctionSet functions, MessageCompaction compaction)
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
				context["time_sent"] = FormatSentTime(assistantMessage.CreatedAt);
				context["content"] = assistantMessage.Content;
				context["attachments"] = GetAttachmentParts(assistantMessage);
				// User-like messages are already gated by user read permissions and their content is always readable
				context["can_read_content"] = assistantMessage.IsUserLike ||
					(permissions.HasFlag(AgentReadPermissions.OtherAgentContent) &&
					exposure.HasFlag(AgentExposureMode.Content));
				bool canReadAttachments = assistantMessage.IsUserLike
					? permissions.HasFlag(AgentReadPermissions.UserAttachments)
					: permissions.HasFlag(AgentReadPermissions.OtherAgentAttachments) && exposure.HasFlag(AgentExposureMode.Attachments);
				context["can_read_attachments"] = canReadAttachments;

				var result = template.Render(context, functions);
				IEnumerable<IAttachment> attachments = [];
				if (canReadAttachments)
					attachments = GetNativeAttachments(assistantMessage);
				return new RCLargeLanguageModels.Messages.UserMessage(agentName, result, attachments);
			}
			else
			{
				var template = templates.GetTextTemplate("foreign_assistant_prompt");

				var context = new Dictionary<string, object?>();
				foreach (var expander in promptMessageContextExpanders)
					expander.ExpandPromptContext(message, agent, context);

				context["agent_name"] = agentName;
				context["time_sent"] = FormatSentTime(assistantMessage.CreatedAt);
				context["reasoning_content"] = compaction.CompactReasoning ? null : assistantMessage.ReasoningContent;
				context["content"] = assistantMessage.Content;
				context["attachments"] = GetAttachmentParts(assistantMessage);
				context["tool_calls"] = assistantMessage.ToolCalls.Select(tc => new
					{
						name = tc.ToolName,
						arguments = tc.Arguments,
						result_content = compaction.ShouldCompactToolCall(tc.CanBeCompacted)
							? MessageCompaction.GetCompactedToolResultContent(tc.Status)
							: tc.ResultContent,
					}).ToArray();

				context["can_read_reasoning"] =
					!compaction.CompactReasoning &&
					permissions.HasFlag(AgentReadPermissions.OtherAgentReasoning) &&
					exposure.HasFlag(AgentExposureMode.Reasoning);
				context["can_read_content"] =
					permissions.HasFlag(AgentReadPermissions.OtherAgentContent) &&
					exposure.HasFlag(AgentExposureMode.Content);
				bool canReadAttachments =
					permissions.HasFlag(AgentReadPermissions.OtherAgentAttachments) && exposure.HasFlag(AgentExposureMode.Attachments);
				context["can_read_attachments"] = canReadAttachments;
				context["can_read_tool_calls"] =
					permissions.HasFlag(AgentReadPermissions.OtherAgentToolCalls) && exposure.HasFlag(AgentExposureMode.ToolCalls);

				var result = template.Render(context, functions);
				IEnumerable<IAttachment> attachments = [];
				if (canReadAttachments)
					attachments = GetNativeAttachments(assistantMessage);
				return new RCLargeLanguageModels.Messages.UserMessage(agentName, result, attachments);
			}
		}

		/// <summary>
		/// Formats a message timestamp with the local time zone offset, e.g. "2026-09-09 21:32:45 (UTC+03:00)".
		/// The <see cref="DateTime"/> value itself does not carry the offset, so it is appended from the local time zone.
		/// </summary>
		private static string FormatSentTime(DateTime time)
		{
			if (time.Kind == DateTimeKind.Utc)
				time = time.ToLocalTime();
			var offset = TimeZoneInfo.Local.GetUtcOffset(time);
			var sign = offset < TimeSpan.Zero ? "-" : "+";
			return $"{time:yyyy-MM-dd HH:mm:ss} (UTC{sign}{offset.Duration():hh\\:mm})";
		}

		private IEnumerable<IMessage> ConvertMessageForAgent(BranchedMessage message,
			ChatAgentDescriptor agent, TemplateFunctionSet functions, MessageCompaction compaction)
		{
			if (message.Message is Domain.UserMessage)
			{
				if (!messageVisibility.IsUserMessageVisibleToAgent(message, agent))
					return [];

				return [BuildUserMessageForAgent(message, agent, functions)];
			}
			else if (message.Message is Domain.AssistantMessage assistantMessage)
			{
				if (!messageVisibility.IsAssistantMessageVisibleToAgent(message, agent))
					return [];

				// Own assistant message — full fidelity with tool calls
				if (assistantMessage.SenderAgentId == agent.Id)
					return BuildOwnAssistantMessageAsMessages(assistantMessage, compaction);

				// Foreign assistant message — merged as quoted user message
				return [BuildForeignAgentMessageText(message, agent, functions, compaction)];
			}
			else if (message.Message is RawUserMessage rawUserMessage)
			{
				var attachments = GetAttachmentParts(rawUserMessage).Select(a => a.NativeAttachment).Where(a => a != null);
				var userMessage = new RCLargeLanguageModels.Messages.UserMessage(Senders.User, rawUserMessage.Content, attachments!);
				return [userMessage];
			}
			else
			{
				throw new InvalidOperationException($"Unsupported message type: {message.GetType()}.");
			}
		}

		private IEnumerable<IMessage> BuildOwnAssistantMessageAsMessages(Domain.AssistantMessage assistantMessage,
			MessageCompaction compaction)
		{
			List<IToolCall> toolCalls = [];
			List<IMessage> messages = [];

			foreach (var toolCall in assistantMessage.ToolCalls)
			{
				toolCalls.Add(new FunctionToolCall(toolCall.ToolCallId, toolCall.ToolName, toolCall.Arguments ?? string.Empty));
				var status = ConvertToolStatus(toolCall.Status);
				var resultContent = compaction.ShouldCompactToolCall(toolCall.CanBeCompacted)
					? MessageCompaction.GetCompactedToolResultContent(toolCall.Status)
					: toolCall.ResultContent ?? string.Empty;
				var toolResult = new ToolResult(status, resultContent,
					GetNativeAttachments(toolCall));
				messages.Add(new ToolMessage(toolResult, toolCall.ToolCallId, toolCall.ToolName));
			}

			var result = new RCLargeLanguageModels.Messages.AssistantMessage(
				assistantMessage.Content ?? string.Empty,
				compaction.CompactReasoning ? string.Empty : assistantMessage.ReasoningContent ?? string.Empty,
				toolCalls: toolCalls,
				attachments: GetNativeAttachments(assistantMessage));
			messages.Insert(0, result);
			
			return messages;
		}

		private static ToolResultStatus ConvertToolStatus(ToolStatus status)
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
	}
}
