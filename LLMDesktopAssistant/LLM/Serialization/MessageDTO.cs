using RCLargeLanguageModels.Messages;

namespace LLMDesktopAssistant.LLM.Serialization
{
	public class MessageDTO
	{
		public string Role { get; set; } = string.Empty;
		public string Sender { get; set; } = string.Empty;
		public string? ReasoningContent { get; set; } = string.Empty;
		public string? Content { get; set; } = string.Empty;
		public string ToolName { get; set; } = string.Empty;
		public string ToolCallId { get; set; } = string.Empty;
		public List<ToolCallDTO> ToolCalls { get; set; } = [];
		public List<AttachmentDTO> Attachments { get; set; } = [];

		public MessageDTO()
		{
		}

		public static MessageDTO ConvertFrom(IMessage message)
		{
			var result = new MessageDTO();

			result.Content = message.Content;
			if (message is IAttachmentsMessage attachmentsMessage)
				result.Attachments = attachmentsMessage.Attachments.Select(AttachmentDTO.ConvertFrom).ToList();

			switch (message)
			{
				case IUserMessage userMessage:
					result.Role = "user";
					break;

				case IAssistantMessage assistantMessage:
					result.Role = "assistant";
					result.ReasoningContent = assistantMessage.ReasoningContent ?? string.Empty;
					result.ToolCalls = assistantMessage.ToolCalls.Select(ToolCallDTO.ConvertFrom).ToList();
					break;

				case IToolMessage toolMessage:
					result.Role = "tool";
					result.ToolCallId = toolMessage.ToolCallId ?? string.Empty;
					result.ToolName = toolMessage.ToolName ?? string.Empty;
					break;

				default:
					throw new ArgumentException($"Unsupported message type: {message.GetType().Name}");

			}

			return result;
		}

		public IMessage ConvertBack()
		{
			var attachments = Attachments.Select(a => a.ConvertBack()).ToList();

			switch (Role)
			{
				case "user":
					return new UserMessage(Sender, Content ?? string.Empty, attachments);

				case "assistant":
					return new AssistantMessage(Content, ReasoningContent,
						ToolCalls.Select(t => t.ConvertBack()), attachments);

				case "tool":
					return new ToolMessage(Content ?? string.Empty, ToolCallId, ToolName, attachments);

				default:
					throw new ArgumentException($"Unsupported role: {Role}");
			}
		}
	}
}