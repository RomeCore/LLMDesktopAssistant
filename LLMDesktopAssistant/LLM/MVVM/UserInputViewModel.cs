using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LLMDesktopAssistant.LLM.MVVM
{
	[ViewModelFor(typeof(UserInputView))]
	public class UserInputViewModel : ViewModelBase
	{
		private string _text = string.Empty;
		/// <summary>
		/// Gets or sets the user input to be sent in the next conversation turn.
		/// </summary>
		public string Text
		{
			get => _text;
			set => SetProperty(ref _text, value);
		}

		/// <summary>
		/// Gets the conversation manager that manages the current conversation.
		/// </summary>
		public Chat Chat { get; }

		/// <summary>
		/// Gets the chat view model that holds this user input manager.
		/// </summary>
		public ChatViewModel ChatViewModel { get; }

		/// <summary>
		/// Command to send a message. This command is bound to the UI and triggers the SendMessage method.
		/// </summary>
		public ICommand SendMessageCommand { get; }

		public UserInputViewModel(ChatViewModel chatVM)
		{
			Chat = chatVM.Chat;
			ChatViewModel = chatVM;
			SendMessageCommand = new AsyncRelayCommand(SendCurrentUserInputAsync);
		}



		public UserInput? Peek()
		{
			if (IsEmpty())
				return null;
			return new UserInput
			{
				Content = _text,
			};
		}

		public UserInput? Pop()
		{
			if (IsEmpty())
				return null;
			var result = new UserInput
			{
				Content = _text,
			};
			Clear();
			return result;
		}

		public void Push(UserInput userInput)
		{

		}

		private void Clear()
		{
			Text = string.Empty;
		}

		public bool IsEmpty()
		{
			return string.IsNullOrWhiteSpace(_text);
		}



		/// <summary>
		/// Sends a message to the LLM and updates the conversation turns.
		/// </summary>
		/// <param name="cts">The cancellation token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		public Task SendCurrentUserInputAsync(CancellationToken cts = default)
		{
			var userInput = Pop();
			if (userInput != null)
				return ChatViewModel.GenerateResponseAsync(userInput, cts);
			return Task.CompletedTask;
		}
	}
}