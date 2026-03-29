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
		private class SendMessageCommandObject : ICommand
		{
			public event EventHandler? CanExecuteChanged;

			private readonly UserInputViewModel _vm;
			public SendMessageCommandObject(UserInputViewModel vm)
			{
				_vm = vm;
				_vm.Chat.SubscribeChanged(nameof(Chat.GenerationCts), _ =>
				{
					InvokeUI(() =>
					{
						CanExecuteChanged?.Invoke(this, EventArgs.Empty);
					});
				});
			}

			public bool CanExecute(object? parameter)
			{
				return _vm.Chat.GenerationCts == null;
			}

			public async void Execute(object? parameter)
			{
				try
				{
					await _vm.SendCurrentUserInputAsync();
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed to send message: {Error}", ex.Message);
				}
			}
		}

		private class CancelGenerationCommandObject : ICommand
		{
			public event EventHandler? CanExecuteChanged;

			private readonly UserInputViewModel _vm;
			public CancelGenerationCommandObject(UserInputViewModel vm)
			{
				_vm = vm;
				_vm.Chat.SubscribeChanged(nameof(Chat.GenerationCts), _ =>
				{
					InvokeUI(() =>
					{
						CanExecuteChanged?.Invoke(this, EventArgs.Empty);
					});
				});
			}

			public bool CanExecute(object? parameter)
			{
				return _vm.Chat.GenerationCts != null;
			}

			public void Execute(object? parameter)
			{
				try
				{
					_vm.Chat.GenerationCts?.Cancel();
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed to cancel generation: {Error}", ex.Message);
				}
			}
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

		/// <summary>
		/// Command to cancel the current generation. This command is bound to the UI and triggers the CancelGeneration method.
		/// </summary>
		public ICommand CancelGenerationCommand { get; }



		private string _text = string.Empty;
		private string _prevText = string.Empty;
		/// <summary>
		/// Gets or sets the user input to be sent in the next conversation turn.
		/// </summary>
		public string Text
		{
			get => _text;
			set => SetProperty(ref _text, value);
		}

		private BranchedMessage? _editingMessage = null;
		/// <summary>
		/// Gets or sets the message that is currently being edited, if any.
		/// </summary>
		public BranchedMessage? EditingMessage
		{
			get => _editingMessage;
			private set => SetProperty(ref _editingMessage, value);
		}

		private bool _isGenerating = false;
		/// <summary>
		/// Gets or sets a value indicating whether the current message is being generated.
		/// </summary>
		public bool IsGenerating
		{
			get => _isGenerating;
			private set => SetProperty(ref _isGenerating, value);
		}

		public UserInputViewModel(ChatViewModel chatVM)
		{
			Chat = chatVM.Chat;
			ChatViewModel = chatVM;
			SendMessageCommand = new SendMessageCommandObject(this);
			CancelGenerationCommand = new CancelGenerationCommandObject(this);

			IsGenerating = Chat.GenerationCts != null;
			Chat.SubscribeChanged(nameof(Chat.GenerationCts), _ =>
			{
				InvokeUI(() =>
				{
					IsGenerating = Chat.GenerationCts != null;
				});
			});
		}



		public UserInput? GetCurrentUserInput()
		{
			if (IsEmpty())
				return null;
			return new UserInput
			{
				Content = _text,
			};
		}

		public void EditMessage(BranchedMessage branchedMessage)
		{
			if (branchedMessage.Message is not UserMessage userMessage)
				throw new ArgumentException("The branched message does not contain a user message.");

			if (EditingMessage != null)
			{
				_prevText = _text;
			}
			EditingMessage = branchedMessage;
			Text = userMessage.Content;
		}

		public void Clear()
		{
			Text = string.Empty;
			EditingMessage = null;
		}

		public void EndEditing()
		{
			Text = _prevText;
			_prevText = string.Empty;
			EditingMessage = null;
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
			var userInput = GetCurrentUserInput();
			var editingMessage = EditingMessage;

			EndEditing();
			if (userInput != null)
			{
				var chatOperator = Chat.Services.GetRequiredService<IChatOperationService>();
				if (editingMessage != null)
					return chatOperator.SendEditedUserInputAsync(editingMessage.MessageIndex, userInput, cts);
				return chatOperator.SendUserInputAsync(userInput, cts);
			}

			return Task.CompletedTask;
		}
	}
}