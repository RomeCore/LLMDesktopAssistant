using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.LLM.Services
{
	[ChatService(typeof(IMessageRevealService))]
	public class MessageRevealService(
		Chat chat
	) : IMessageRevealService
	{
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