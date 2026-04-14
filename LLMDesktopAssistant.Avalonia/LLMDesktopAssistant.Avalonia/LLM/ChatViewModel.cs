using LLMDesktopAssistant.Avalonia.LLM.Messages;
using LLMDesktopAssistant.Core.LLM.Domain;

namespace LLMDesktopAssistant.Avalonia.LLM
{
	[ViewModelFor(typeof(ChatView))]
	public class ChatViewModel : ViewModelBase
	{
		/// <summary>
		/// Gets the message sequence that represents the conversation history.
		/// </summary>
		public MessageSequenceViewModel MessageSequence { get; }

		/// <summary>
		/// Gets the conversation manager that manages the current conversation.
		/// </summary>
		public Chat Chat { get; }

		/// <summary>
		/// Gets the user input to be sent in the next conversation turn.
		/// </summary>
		public UserInputViewModel UserInput { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatViewModel"/> class.
		/// </summary>
		public ChatViewModel(Chat chat)
		{
			Chat = chat;
			UserInput = new UserInputViewModel(this);
			MessageSequence = new MessageSequenceViewModel(this);
		}
	}
}