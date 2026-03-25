using LLMDesktopAssistant.LLM.Domain;
using RCLargeLanguageModels.Messages;

namespace LLMDesktopAssistant.LLM.Services
{
	public class PromptChatBuilder(
		Chat chat,
		IMessageConverter messageConverter
		) : IPromptChatBuilder
	{
		public IEnumerable<IMessage> Build()
		{
			List<IMessage> messages = [];

			string systemPrompt = chat.SystemPrompt ?? "You are a helpful assistant.";

			for (int i = chat.Messages.Count - 1; i >= 0; i--)
			{
				var message = chat.Messages[i].Message;

				if (!string.IsNullOrWhiteSpace(message.SummaryOfPrevMessages))
				{
					systemPrompt = $"""
						{systemPrompt}

						There is summary of previous messages in the conversation:
						{message.SummaryOfPrevMessages}
						""";
					break;
				}

				messages.InsertRange(0, messageConverter.Convert(message));
			}

			messages.Insert(0, new SystemMessage(systemPrompt));

			return messages;
		}
	}
}