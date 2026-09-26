using System.Text;
using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.Settings;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM.Additional;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Prompting.Context;
using LLMDesktopAssistant.Prompting.ContextExpanders;
using LLMDesktopAssistant.Prompting.Hooks;
using LLMDesktopAssistant.Prompting.Plugins;
using LLMDesktopAssistant.Users;
using LLTSharp;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Messages.Attachments;
using RCLargeLanguageModels.Tools;
using Serilog;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <inheritdoc cref="IAgentPromptComposer"/>
	[ChatService(typeof(IAgentPromptComposer))]
	public class AgentPromptComposer(
		Chat chat,
		IChatSettingsService chatSettings,
		ITemplateLibraryAccessor templates,
		IAgentManagementService agentManager,
		IMessageVisibilityService messageVisibility,
		IAgentEffectiveMessagesProvider effectiveMessagesProvider,
		IUserManagementService userManager,
		IEnumerable<IPromptBuildingHook> promptBuildingHooks,
		IEnumerable<IPromptMessageContextExpander> promptMessageContextExpanders,
		IEnumerable<IPromptTemplatePlugin> promptTemplatePlugins,
		IToolsetCacheService toolsetCache,
		IPromptAnchoredSectionProcessor promptAnchoredSectionProcessor,
		IPromptSupersedeContextProcessor promptSupersedeContextProcessor,
		IAddonSetCollector<PromptContextInfo> promptContextCollector,
		IPromptDumpService promptDumpService
		) : IAgentPromptComposer
	{
		private const string summaryTag = "summary";
		private const string systemReminderTag = "system-reminder";

		/// <inheritdoc/>
		public AgentPromptBundle Build(ChatAgentDescriptor agent)
		{
			// The composer owns the toolset cache: it must be fresh for both the tool definitions
			// and the tool call resolution performed by the execution service.
			toolsetCache.Invalidate(agent);

			var effectiveContext = effectiveMessagesProvider.GetEffectiveMessages(agent);

			var hooks = promptBuildingHooks.OrderBy(h => h.Order).ToList();
			var functions = new TemplateFunctionSet(promptTemplatePlugins.SelectMany(p => p.GetTemplateFunctions()));

			List<IMessage> result = [];
			var disabledCheckpoints = agent.Context.GetEffectiveDisabledFlags(chatSettings.Settings);

			var promptContextInfos = promptContextCollector.GetAddonsForAgent(agent);
			var promptContextProviders = promptContextInfos.Select(i => i.Provider).ToArray();

			var promptMode = agent.Context.PromptMode;
			var anchor = promptAnchoredSectionProcessor.Process(agent, effectiveContext, promptContextProviders.Anchored());
			promptSupersedeContextProcessor.Process(agent, effectiveContext, promptContextProviders.Supersede());

			SystemPromptSnapshot header;
			string headerSource;
			switch (promptMode)
			{
				case PromptContextMode.Static:
					var settings = agent.Context;
					if (settings.Snapshot is null)
					{
						settings.Snapshot = promptContextProviders.Anchored().RenderHeader(agent);
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

			var summaryCheckpoint = effectiveContext.Checkpoints.LastOrDefault(c =>
				(c.Checkpoint.Kind & ~disabledCheckpoints).HasFlag(ContextCheckpointKind.Summary));

			result.Add(new SystemMessage(header.Text));
			if (summaryCheckpoint != null)
				result.Add(new RCLargeLanguageModels.Messages.UserMessage(Senders.User, $"""
					<{summaryTag}>
					{summaryCheckpoint.Checkpoint.Context}
					</{summaryTag}>
					"""));

			// SCM stamps: walk up beyond effective message history (but including one effective message)
			// to find the most recent stamp of each type.
			Dictionary<string, (int MsgId, int Order, PromptSupersedeStampBase Stamp)>? seenStamps = [];
			for (int i = effectiveContext.EffectiveMessagesStartIndex; i >= 0; i--)
			{
				var branchedMessage = chat.Messages[i];

				if (branchedMessage.Message is not Domain.AssistantMessage assistantMessage || assistantMessage.SenderAgentId != agent.Id)
					continue;
				if (assistantMessage.AdditionalData.TryGet<PromptSupersedeStampMessageData>() is not { } stampData)
					continue;

				foreach (var stamp in stampData.Stamps)
				{
					if (seenStamps.ContainsKey(stamp.Discriminator))
						continue;

					seenStamps[stamp.Discriminator] = (i, seenStamps.Count, stamp);
				}
			}

			for (int i = 0; i < effectiveContext.Messages.Count; i++)
			{
				var branchedMessage = effectiveContext.Messages[i];
				var compaction = MessageCompaction.ForMessage(effectiveContext.Checkpoints, i, disabledCheckpoints);

				IEnumerable<IMessage> messages;
				bool isPendingAssistant = false;
				if (branchedMessage.Message is Domain.AssistantMessage assistantMessage && !assistantMessage.IsCompleted)
				{
					isPendingAssistant = true;
					messages = [];
				}
				else
				{
					messages = ConvertMessageForAgent(branchedMessage, agent, functions, compaction);
				}

				foreach (var hook in hooks)
				{
					var editedMessages = hook.ModifyFinalContext(messages, branchedMessage, agent);
					if (editedMessages != null)
						messages = editedMessages;
				}

				if (branchedMessage.Message is Domain.AssistantMessage)
				{
					var systemReminderSb = new StringBuilder();
					systemReminderSb.AppendLine($"<{systemReminderTag}>");
					int dataCounter = 0;

					// Process SCM anchor deltas.
					if (anchor is not null && branchedMessage.Message.AdditionalData.TryGet<PromptStateDeltaMessageData>() is { } deltas)
					{
						if (deltas.AnchorId == anchor.Id && !string.IsNullOrWhiteSpace(deltas.Snapshot))
						{
							systemReminderSb.AppendLine(deltas.Snapshot);
							dataCounter++;
						}
					}

					// Process SCM supersede stamps.
					if (seenStamps != null)
					{
						// Render stamps if the message is the first message after cut.
						// This should include the stamps before the cut.
						foreach (var (_, _, stamp) in seenStamps.Values
							.OrderBy(s => s.MsgId)
							.ThenBy(s => s.Order))
						{
							systemReminderSb.AppendLine(stamp.Snapshot);
							dataCounter++;
						}
						seenStamps = null;
					}
					else if (branchedMessage.Message.AdditionalData.TryGet<PromptSupersedeStampMessageData>() is { } stamps)
					{
						foreach (var stamp in stamps.Stamps)
						{
							systemReminderSb.AppendLine(stamp.Snapshot);
							dataCounter++;
						}
					}

					// Process SCM live tails.
					if (isPendingAssistant)
					{
						var liveContextProviders = promptContextProviders.LiveTails().ToArray();
						if (liveContextProviders.Length > 0)
						{
							var sb = new StringBuilder();
							foreach (var provider in liveContextProviders)
							{
								var liveContext = provider.Provide(effectiveContext);
								if (!string.IsNullOrWhiteSpace(liveContext))
								{
									systemReminderSb.AppendLine(liveContext);
									dataCounter++;
								}
							}
						}
					}

					systemReminderSb.Append($"</{systemReminderTag}>");

					if (dataCounter > 0)
					{
						if (result.Count > 0 && result[^1] is IToolMessage lastToolMessage)
						{
							// Replace the last tool result with the new one, appending the system reminder.
							// This helps to avoid interrupting the assistant's tool cycle.
							var lastToolResult = lastToolMessage.Result;
							var replacedToolResult = new RCLargeLanguageModels.Tools.ToolResult(lastToolResult.Status,
								$"""
								{lastToolResult.Content}
								{systemReminderSb}
								""", lastToolResult.Attachments);
							result[^1] = new RCLargeLanguageModels.Messages.ToolMessage(replacedToolResult,
								lastToolMessage.ToolCallId, lastToolMessage.ToolName);
						}
						else
						{
							result.Add(new RCLargeLanguageModels.Messages.UserMessage(systemReminderSb.ToString()));
						}
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
