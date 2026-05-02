using Avalonia.Collections;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.UIExtensions.MessageExtensions;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Messages
{
	[ViewModelFor(typeof(UserMessageView))]
	public class UserMessageViewModel : MessageViewModelBase
	{
		private readonly UserMessage _userMessage;
		public UserMessage UserMessage => _userMessage;

		private string _text = string.Empty;
		public string Text
		{
			get => _text;
			set => SetProperty(ref _text, value);
		}

		private readonly AvaloniaList<Attachment> _attachments = [];
		public ICollection<Attachment> Attachments
		{
			get => _attachments;
			set
			{
				_attachments.Clear();
				_attachments.AddRange(value);
			}
		}

		public ImmutableList<MessageExtension> Extensions { get; }

		public ICommand EditCommand { get; }

		public UserMessageViewModel(BranchedMessage branchedMessage, ChatViewModel chatVM) : base(branchedMessage, chatVM)
		{
			if (branchedMessage.Message is not UserMessage userMessage)
				throw new InvalidOperationException("Invalid message type. Expected IUserMessage.");

			_userMessage = userMessage;
			Text = userMessage.Content ?? string.Empty;
			Attachments = userMessage.Attachments;
			Extensions = MessageExtensionManager.CreateExtensions(this, chatVM.Chat);

			EditCommand = new RelayCommand(() =>
			{
				chatVM.UserInput.EditMessage(branchedMessage);
			});
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				foreach (var extension in Extensions)
					extension.Dispose();
			}
		}
	}
}