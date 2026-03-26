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

		public UserMessageViewModel(BranchedMessage branchedMessage, Chat chat) : base(branchedMessage, chat)
		{
			if (branchedMessage.Message is not UserMessage userMessage)
				throw new InvalidOperationException("Invalid message type. Expected IUserMessage.");

			Text = userMessage.Content ?? string.Empty;
		}
	}
}