using Avalonia.Collections;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.UIExtensions.MessageExtensions;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.WebUI;

namespace LLMDesktopAssistant.LLM.Messages
{
	[ViewModelFor(typeof(UserMessageView))]
	public class UserMessageViewModel : MessageViewModelBase
	{
		private readonly UserMessage _userMessage;
		public UserMessage UserMessage => _userMessage;

		public bool ShowAvatar { get; }
		public Bitmap? SenderAvatar { get; }
		public string? SenderName { get; }

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

			// Determine that we can apply avatar
			var prevMessage = branchedMessage.MessageIndex - 1 >= 0
				? chatVM.Chat.Messages[branchedMessage.MessageIndex - 1].Message as UserMessage
				: null;
			if (prevMessage == null || userMessage.SenderLogin != prevMessage.SenderLogin)
			{
				var userManager = chatVM.Chat.Services.GetRequiredService<IUserManagementService>();
				var user = userManager.FindByLogin(userMessage.SenderLogin);

				try
				{
					if (!string.IsNullOrWhiteSpace(user?.Base64ProfileImage))
					{
						var bytes = Convert.FromBase64String(user.Base64ProfileImage);
						using var ms = new MemoryStream(bytes);
						SenderAvatar = new Bitmap(ms);
					}
				}
				catch
				{
					SenderAvatar = null;
				}
				SenderName = user?.Name ?? userMessage.SenderLogin;
				ShowAvatar = true;
			}

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