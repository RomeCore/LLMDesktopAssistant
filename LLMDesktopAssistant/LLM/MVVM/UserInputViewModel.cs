using Avalonia.Collections;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.DIalogs;
using LLMDesktopAssistant.LLM.Attachments;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.MCP;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using Serilog;
using LLMDesktopAssistant.LLM.Services.Users;

namespace LLMDesktopAssistant.LLM.MVVM
{
	public class UserMessageVisibilityItemModel
	{
		public required MessageVisibility Visibility { get; init; }
		public required string Title { get; init; }
	}

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

		private class CancelEditCommandObject : ICommand
		{
			public event EventHandler? CanExecuteChanged;

			private readonly UserInputViewModel _vm;
			public CancelEditCommandObject(UserInputViewModel vm)
			{
				_vm = vm;
				_vm.SubscribeChanged(nameof(UserInputViewModel.EditingMessage), _ =>
				{
					InvokeUI(() =>
					{
						CanExecuteChanged?.Invoke(this, EventArgs.Empty);
					});
				});
			}

			public bool CanExecute(object? parameter)
			{
				return _vm.EditingMessage != null;
			}

			public void Execute(object? parameter)
			{
				try
				{
					_vm.EndEditing();
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed to cancel edit: {Error}", ex.Message);
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
		/// Command to open settings.
		/// </summary>
		public ICommand OpenSettingsCommand { get; }

		/// <summary>
		/// Command to open MCP manager.
		/// </summary>
		public ICommand OpenMCPManagerCommand { get; }
		/// <summary>
		/// Command to open prompt manager.
		/// </summary>
		public ICommand OpenPromptManagerCommand { get; }

		/// <summary>
		/// Command to open attachments manager.
		/// </summary>
		public ICommand OpenAttachmentsManagerCommand { get; }

		/// <summary>
		/// Command to send a message.
		/// </summary>
		public ICommand SendMessageCommand { get; }

		/// <summary>
		/// Command to cancel edit of the current message.
		/// </summary>
		public ICommand CancelEditCommand { get; }

		/// <summary>
		/// Command to cancel the current generation.
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

		private readonly AvaloniaList<AttachmentViewModel> _attachments = [];
		private ImmutableList<Attachment> _prevAttachments = [];
		/// <summary>
		/// Gets or sets the attachments or additional buttons to be displayed with the current message.
		/// </summary>
		public ICollection<AttachmentViewModel> Attachments
		{
			get => _attachments;
			set
			{
				_attachments.Clear();
				_attachments.AddRange(value);
			}
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

		public ImmutableList<UserMessageVisibilityItemModel> Visibilities { get; } = [
			new UserMessageVisibilityItemModel { Visibility = MessageVisibility.Always, Title = "message_visibility_always" },
			new UserMessageVisibilityItemModel { Visibility = MessageVisibility.RevealAfterSend, Title = "message_visibility_reveal_after_send" },
			new UserMessageVisibilityItemModel { Visibility = MessageVisibility.OnlyUsers, Title = "message_visibility_only_users" },
			new UserMessageVisibilityItemModel { Visibility = MessageVisibility.OnlyAgents, Title = "message_visibility_only_agents" }
		];

		private UserMessageVisibilityItemModel _selectedVisibility;
		/// <summary>
		/// Gets or sets the visibility of the next user message.
		/// </summary>
		public UserMessageVisibilityItemModel SelectedVisibility
		{
			get => _selectedVisibility;
			set => SetProperty(ref _selectedVisibility, value);
		}




		public UserInputViewModel(ChatViewModel chatVM)
		{
			Chat = chatVM.Chat;
			ChatViewModel = chatVM;

			OpenSettingsCommand = new AsyncRelayCommand(async () =>
			{
				var viewModel = new SettingsCategoryViewModel<ChatSettings>(cs => new ChatSettingsViewModel(cs, Chat),
					newSettings => Chat.Settings = newSettings, Chat.Settings.Id);
				await DialogManager.ShowDialogAsync(viewModel);
			});

			OpenMCPManagerCommand = new AsyncRelayCommand(async () =>
			{
				var viewModel = new MCPManagerViewModel();
				await DialogManager.ShowDialogAsync(viewModel);
			});

			OpenPromptManagerCommand = new AsyncRelayCommand(async () =>
			{
				var viewModel = new PromptManagerViewModel();
				await DialogManager.ShowDialogAsync(viewModel);
			});

			OpenAttachmentsManagerCommand = new AsyncRelayCommand(async () =>
			{
				var viewModel = new AttachmentsManagerViewModel(this);
				await DialogManager.ShowDialogAsync(viewModel);
			});

			SendMessageCommand = new SendMessageCommandObject(this);
			CancelEditCommand = new CancelEditCommandObject(this);
			CancelGenerationCommand = new CancelGenerationCommandObject(this);

			IsGenerating = Chat.GenerationCts != null;
			Chat.SubscribeChanged(nameof(Chat.GenerationCts), _ =>
			{
				InvokeUI(() =>
				{
					IsGenerating = Chat.GenerationCts != null;
				});
			});

			_selectedVisibility = Visibilities[0];
		}



		public UserInput? GetCurrentUserInput()
		{
			if (IsEmpty())
				return null;

			var userManager = Chat.Services.GetRequiredService<IUserManagementService>();

			return new UserInput
			{
				Content = _text,
				SenderLogin = userManager.GetAllUsers().FirstOrDefault()?.Login ?? "user",
				Attachments = _attachments.Select(a => a.Attachment).ToImmutableList(),
				Visibility = _selectedVisibility.Visibility,
			};
		}

		public void EditMessage(BranchedMessage branchedMessage)
		{
			if (branchedMessage.Message is not UserMessage userMessage)
				throw new ArgumentException("The branched message does not contain a user message.");

			if (EditingMessage != null)
			{
				_prevText = _text;
				_prevAttachments = _attachments.Select(am => am.Attachment).ToImmutableList();
			}
			EditingMessage = branchedMessage;
			Text = userMessage.Content;
			Attachments = userMessage.Attachments.Select(a => new AttachmentViewModel(this, a)).ToList();
		}

		public void Clear()
		{
			Text = string.Empty;
			Attachments = [];
			EditingMessage = null;
		}

		public void EndEditing()
		{
			Text = _prevText;
			Attachments = _prevAttachments.Select(a => new AttachmentViewModel(this, a)).ToList();
			_prevText = string.Empty;
			_prevAttachments = [];
			EditingMessage = null;
		}

		public bool IsEmpty()
		{
			return string.IsNullOrWhiteSpace(_text) && Attachments.Count == 0;
		}

		public async Task AcceptDropAsync(DragEventArgs args)
		{
			var viewModel = new AttachmentsManagerViewModel(this);
			viewModel.AcceptDrop(args);
			await DialogManager.ShowDialogAsync(viewModel);
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