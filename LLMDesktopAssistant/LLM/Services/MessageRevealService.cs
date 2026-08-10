using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.Services
{
	[ChatService(typeof(IChatExecutionHook))]
	[ChatService(typeof(IMessageRevealService))]
	public class MessageRevealService(
		Chat chat
	) : IMessageRevealService, IChatExecutionHook
	{
		public Task OnResponsePrepareAsync(ChatPrepareExecutionHookContext context, CancellationToken cancellationToken = default)
		{
			RevealMessages();
			return Task.CompletedTask;
		}

		public void RevealMessages()
		{
			for (int i = chat.Messages.Count - 1; i >=0; i--)
			{
				var message = chat.Messages[i].Message;
				if (message is UserMessage userMessage)
				{
					if (userMessage.IsRevealed)
						return; // We encountered already revealed message, so we stop here.
					userMessage.IsRevealed = true;
				}
			}
		}
	}
}