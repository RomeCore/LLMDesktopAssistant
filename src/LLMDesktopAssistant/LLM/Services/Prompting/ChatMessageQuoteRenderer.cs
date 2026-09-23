using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM.Additional;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Prompting.ContextExpanders;
using LLMDesktopAssistant.Prompting.Hooks;
using LLMDesktopAssistant.Prompting.Plugins;
using LLMDesktopAssistant.Users;
using LLTSharp;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	/// <inheritdoc cref="IChatMessageQuoteRenderer"/>
	[ChatService(typeof(IChatMessageQuoteRenderer))]
	public class ChatMessageQuoteRenderer(
		ITemplateLibraryAccessor templates,
		IUserManagementService userManager,
		IAgentManagementService agentManager,
		IEnumerable<IPromptSystemContextExpander> promptSystemContextExpanders,
		IEnumerable<IPromptMessageContextExpander> promptMessageContextExpanders,
		IEnumerable<IPromptTemplatePlugin> promptTemplatePlugins
		) : IChatMessageQuoteRenderer
	{
		/// <inheritdoc/>
		public string RenderQuote(BranchedMessage message)
		{
			var functions = new TemplateFunctionSet(promptTemplatePlugins.SelectMany(p => p.GetTemplateFunctions()));

			switch (message.Message)
			{
				case RawUserMessage rawUserMessage:
					return rawUserMessage.Content;

				case Domain.UserMessage userMessage:
					{
						var name = userManager.FindByLogin(userMessage.SenderLogin)?.GetAgentShownName() ?? userMessage.SenderLogin;
						return RenderUserStyleQuote(message, name, userMessage.CreatedAt, userMessage.Content, functions);
					}

				case Domain.AssistantMessage assistantMessage:
					{
						var senderDescriptor = agentManager.GetAgentDescriptor(assistantMessage.SenderAgentId);
						var name = senderDescriptor.Info.Name ?? senderDescriptor.Id.ToString()[..8];
						return RenderUserStyleQuote(message, name, assistantMessage.CreatedAt, assistantMessage.Content ?? string.Empty, functions);
					}

				default:
					throw new InvalidOperationException($"Unsupported message type: {message.GetType()}.");
			}
		}

		/// <summary>
		/// Renders any message as a neutral user-style content quote.
		/// </summary>
		private string RenderUserStyleQuote(BranchedMessage message, string name, DateTime time, string content,
			TemplateFunctionSet functions)
		{
			var template = templates.GetTextTemplate("user_message_prompt");

			var context = new Dictionary<string, object?>();
			foreach (var expander in promptSystemContextExpanders)
				expander.ExpandPromptContext(context);
			foreach (var expander in promptMessageContextExpanders)
				expander.ExpandPromptContext(message, null, context);

			context["user_name"] = name;
			context["time_sent"] = FormatSentTime(time);
			context["content"] = content;
			context["attachments"] = message.Message.AdditionalData.GetAll<AttachmentMessagePart>();
			context["can_read_content"] = true;
			context["can_read_attachments"] = true;

			return template.Render(context, functions).ToString();
		}

		/// <summary>
		/// Formats a message timestamp with the local time zone offset, e.g. "2026-09-09 21:32:45 (UTC+03:00)".
		/// </summary>
		private static string FormatSentTime(DateTime time)
		{
			if (time.Kind == DateTimeKind.Utc)
				time = time.ToLocalTime();
			var offset = TimeZoneInfo.Local.GetUtcOffset(time);
			var sign = offset < TimeSpan.Zero ? "-" : "+";
			return $"{time:yyyy-MM-dd HH:mm:ss} (UTC{sign}{offset.Duration():hh\\:mm})";
		}
	}
}
