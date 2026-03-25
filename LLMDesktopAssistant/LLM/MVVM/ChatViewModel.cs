using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Data;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.ToolModules;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace LLMDesktopAssistant.LLM.MVVM
{
	[ViewModelFor(typeof(ChatView))]
	public class ChatViewModel : ViewModelBase
	{
		private CancellationTokenSource? _sendCts;

		/// <summary>
		/// Gets the message sequence that represents the conversation history.
		/// </summary>
		public MessageSequenceViewModel MessageSequence { get; }

		/// <summary>
		/// Gets the conversation manager that manages the current conversation.
		/// </summary>
		public Chat Chat { get; }

		private string _userInput = string.Empty;
		/// <summary>
		/// Gets or sets the user input to be sent in the next conversation turn.
		/// </summary>
		public string UserInput
		{
			get => _userInput;
			set => SetProperty(ref _userInput, value);
		}

		/// <summary>
		/// Command to send a message. This command is bound to the UI and triggers the SendMessage method.
		/// </summary>
		public ICommand SendMessageCommand { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatViewModel"/> class.
		/// </summary>
		public ChatViewModel(Chat chat)
		{
			SendMessageCommand = new AsyncRelayCommand(async ct =>
			{
				try
				{
					await SendCurrentUserInputAsync(ct);
				}
				catch (Exception ex)
				{
					Log.Error(ex, "An error occurred while sending a message: {error}.", ex);
				}
				finally
				{
					// TODO
					// Turns.Last().State = ConversationTurnState.Complete;
				}
			});

			Chat = chat;
			MessageSequence = new MessageSequenceViewModel(chat);
		}

		/// <summary>
		/// Sends a message to the LLM and updates the conversation turns.
		/// </summary>
		/// <param name="userMessage">The user message to be sent.</param>
		/// <param name="cts">The cancellation token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		public async Task GenerateResponseAsync(CancellationToken cts = default)
		{
			try
			{
				_sendCts?.Cancel(); // Cancel any previous send operation
				_sendCts = CancellationTokenSource.CreateLinkedTokenSource(cts);
				cts = _sendCts.Token;

				var chatOperator = Chat.Services.GetRequiredService<IChatOperationService>();
				await chatOperator.ContinueGenerationAsync(cts);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "An error occurred while sending a message: {error}.", ex);
			}
		}

		/// <summary>
		/// Sends a message to the LLM and updates the conversation turns.
		/// </summary>
		/// <param name="userMessage">The user message to be sent.</param>
		/// <param name="cts">The cancellation token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		public async Task GenerateResponseAsync(UserInput userInput, CancellationToken cts = default)
		{
			try
			{
				_sendCts?.Cancel(); // Cancel any previous send operation
				_sendCts = CancellationTokenSource.CreateLinkedTokenSource(cts);
				cts = _sendCts.Token;

				var chatOperator = Chat.Services.GetRequiredService<IChatOperationService>();
				await chatOperator.SendUserInputAsync(userInput, cts);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "An error occurred while sending a message: {error}.", ex);
			}
		}

		/// <summary>
		/// Sends a message to the LLM and updates the conversation turns.
		/// </summary>
		/// <param name="cts">The cancellation token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		public Task SendCurrentUserInputAsync(CancellationToken cts = default)
		{
			var userInput = new UserInput
			{
				Content = _userInput
			};
			UserInput = string.Empty;
			return GenerateResponseAsync(userInput, cts);
		}
	}
}