using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Data;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;

namespace LLMDesktopAssistant.LLM.MVVM.Messages
{
	[ViewModelFor(typeof(UserMessageView))]
	public class UserMessageViewModel : MessageViewModelBase
	{
		private string _text = string.Empty;
		public string Text
		{
			get => _text;
			set => SetProperty(ref _text, value);
		}

		private readonly RangeObservableCollection<Attachment> _attachments = [];
		public ICollection<Attachment> Attachments
		{
			get => _attachments;
			set => _attachments.Reset(value);
		}

		public ICommand EditCommand { get; }

		public UserMessageViewModel(BranchedMessage branchedMessage, ChatViewModel chatVM) : base(branchedMessage, chatVM)
		{
			if (branchedMessage.Message is not UserMessage userMessage)
				throw new InvalidOperationException("Invalid message type. Expected IUserMessage.");

			Text = userMessage.Content ?? string.Empty;
			Attachments = userMessage.Attachments;

			EditCommand = new RelayCommand(() =>
			{
				chatVM.UserInput.EditMessage(branchedMessage);
			});
		}
	}
}