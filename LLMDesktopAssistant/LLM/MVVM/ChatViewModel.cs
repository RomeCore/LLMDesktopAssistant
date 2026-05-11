using LLMDesktopAssistant.LLM.Messages;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM;

namespace LLMDesktopAssistant.LLM.MVVM
{
	[ViewModelFor(typeof(ChatView))]
	public class ChatViewModel : ViewModelBase
	{
		/// <summary>
		/// Gets the message sequence that represents the conversation history.
		/// </summary>
		public MessageSequenceViewModel MessageSequence { get; }

		/// <summary>
		/// Gets the chat status view model.
		/// </summary>
		public ChatStatusViewModel ChatStatus { get; }

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
			ChatStatus = new ChatStatusViewModel(chat);
			MessageSequence = new MessageSequenceViewModel(this);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				MessageSequence.Dispose();
				UserInput.Dispose();
				ChatStatus.Dispose();

			}
		}
	}
}