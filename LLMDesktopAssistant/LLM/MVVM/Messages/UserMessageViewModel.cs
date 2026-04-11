using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Core.LLM.Data;
using LLMDesktopAssistant.Core.LLM.Domain;
using LLMDesktopAssistant.Core.LLM.Services;
using LLMDesktopAssistant.Core.MVVM;
using LLMDesktopAssistant.Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;

namespace LLMDesktopAssistant.Core.LLM.MVVM.Messages
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