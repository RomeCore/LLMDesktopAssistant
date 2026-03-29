using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Data;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.MVVM;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;

namespace LLMDesktopAssistant.LLM.MVVM
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

		public ICommand EditCommand { get; }

		public UserMessageViewModel(BranchedMessage branchedMessage, ChatViewModel chatVM) : base(branchedMessage, chatVM)
		{
			if (branchedMessage.Message is not UserMessage userMessage)
				throw new InvalidOperationException("Invalid message type. Expected IUserMessage.");

			Text = userMessage.Content ?? string.Empty;

			EditCommand = new RelayCommand(() =>
			{
				chatVM.UserInput.EditMessage(branchedMessage);
			});
		}
	}
}