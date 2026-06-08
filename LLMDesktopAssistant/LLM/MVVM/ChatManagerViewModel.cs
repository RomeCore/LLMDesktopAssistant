using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using LiveMarkdown.Avalonia;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Localization.Resources;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.WebUI;
using Serilog;

namespace LLMDesktopAssistant.LLM.MVVM
{
	public class AvailableChatViewModel : NotifyPropertyChanged
	{
		public required int Id { get; init; }
		public required ICommand DeleteCommand { get; init; }

		private string _title = string.Empty;
		public string Title
		{
			get => _title;
			set => SetProperty(ref _title, value);
		}

		private string _topic = string.Empty;
		public string Topic
		{
			get => _topic;
			set
			{
				if (SetProperty(ref _topic, value))
					RaisePropertyChanged(nameof(TopicColorHex));
			}
		}

		private DateTime _lastModifiedAt = DateTime.Now;
		public DateTime LastModifiedAt
		{
			get => _lastModifiedAt;
			set => SetProperty(ref _lastModifiedAt, value);
		}

		private bool _isSelected = false;
		public bool IsSelected
		{
			get => _isSelected;
			set => SetProperty(ref _isSelected, value);
		}



		public string TopicColorHex => GenerateColorHexFromHash(Topic);

		private static string GenerateColorHexFromHash(string topic)
		{
			if (string.IsNullOrEmpty(topic))
				return "#808080";

			var hash = GetDeterministicHashCode(topic);

			const double min = 0.35;
			const double range = 0.65;

			double rh = (hash * 397 % 255) / 255.0;
			byte r2 = (byte)(min * 255 + rh * range * 255);
			double rg = (hash * 137 * 397 % 255) / 255.0;
			byte g2 = (byte)(min * 255 + rg * range * 255);
			double rb = (hash * 37 * 397 % 255) / 255.0;
			byte b2 = (byte)(min * 255 + rb * range * 255);

			return $"#{r2:X2}{g2:X2}{b2:X2}";
		}

		private static int GetDeterministicHashCode(string str)
		{
			unchecked
			{
				int hash1 = (5381 << 16) + 5381;
				int hash2 = hash1;

				for (int i = 0; i < str.Length; i += 2)
				{
					hash1 = ((hash1 << 5) + hash1) ^ str[i];
					if (i + 1 < str.Length)
						hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
				}

				return hash1 + hash2 * 1566083941;
			}
		}



		public static AvailableChatViewModel CreateFromInfo(ChatInfo info, ICommand deleteCommand)
		{
			return new AvailableChatViewModel
			{
				Id = info.Id,
				DeleteCommand = deleteCommand,
				Title = info.Title,
				Topic = info.Topic,
				LastModifiedAt = info.LastModifiedAt
			};
		}
	}

	public class OpenedChatViewModel : NotifyPropertyChanged
	{
		public required IChatManagementService ChatManager { get; init; }

		private IServiceScope? _currentChatScope;
		private ChatViewModel? _currentChat;
		public ChatViewModel? CurrentChat
		{
			get => _currentChat;
			private set => SetProperty(ref _currentChat, value);
		}

		private AvailableChatViewModel? _selectedAvailable;
		public AvailableChatViewModel? SelectedAvailable
		{
			get => _selectedAvailable;
			set
			{
				if (_selectedAvailable != value)
				{
					if (_selectedAvailable != null)
						UnlockAvailableChat(_selectedAvailable, _currentChat!.Chat);

					if (SetProperty(ref _selectedAvailable, value) && value != null)
						OpenConversation(_selectedAvailable!);
				}
			}
		}

		public bool IsEditing => IsEditingTitle || IsEditingTopic;

		private bool _isEditingTitle = false;
		public bool IsEditingTitle
		{
			get => _isEditingTitle;
			set
			{
				if (SetProperty(ref _isEditingTitle, value))
					RaisePropertyChanged(nameof(IsEditing));
			}
		}

		private string _editTitleText = string.Empty;
		public string EditTitleText
		{
			get => _editTitleText;
			set => SetProperty(ref _editTitleText, value);
		}

		private bool _isEditingTopic = false;
		public bool IsEditingTopic
		{
			get => _isEditingTopic;
			set
			{
				if (SetProperty(ref _isEditingTopic, value))
					RaisePropertyChanged(nameof(IsEditing));
			}
		}

		private string _editTopicText = string.Empty;
		public string EditTopicText
		{
			get => _editTopicText;
			set => SetProperty(ref _editTopicText, value);
		}

		public ICommand StartEditTitleCommand { get; }
		public ICommand CommitEditTitleCommand { get; }
		public ICommand CancelEditTitleCommand { get; }

		public ICommand StartEditTopicCommand { get; }
		public ICommand CommitEditTopicCommand { get; }
		public ICommand CancelEditTopicCommand { get; }

		public ICommand RenameWithAICommand { get; }
		public required ICommand CloseChatCommand { get; init; }

		public OpenedChatViewModel()
		{
			StartEditTitleCommand = new RelayCommand(() =>
			{
				if (CurrentChat?.Chat != null)
				{
					EditTitleText = CurrentChat.Chat.Title;
					IsEditingTitle = true;
				}
			});

			CommitEditTitleCommand = new RelayCommand(() =>
			{
				if (CurrentChat?.Chat != null && !string.IsNullOrWhiteSpace(EditTitleText))
					CurrentChat.Chat.Title = EditTitleText.Trim();
				IsEditingTitle = false;
			});

			CancelEditTitleCommand = new RelayCommand(() =>
			{
				IsEditingTitle = false;
			});

			StartEditTopicCommand = new RelayCommand(() =>
			{
				if (CurrentChat?.Chat != null)
				{
					EditTopicText = CurrentChat.Chat.Topic;
					IsEditingTopic = true;
				}
			});

			CommitEditTopicCommand = new RelayCommand(() =>
			{
				if (CurrentChat?.Chat != null)
					CurrentChat.Chat.Topic = EditTopicText.Trim();
				IsEditingTopic = false;
			});

			CancelEditTopicCommand = new RelayCommand(() =>
			{
				IsEditingTopic = false;
			});

			RenameWithAICommand = new AsyncRelayCommand(async () =>
			{
				if (CurrentChat?.Chat != null)
				{
					try
					{
						if (CurrentChat.Chat.Messages.Count == 0)
						{
							var toastService = CurrentChat.Chat.Services.GetService<IToastService>();
							if (toastService != null)
								toastService.ShowWarning(LocalizationManager.LocalizeStatic("chat_manager_no_messages_to_rename"),
									LocalizationManager.LocalizeStatic("chat_manager_no_messages_to_rename_desc"));
							return;
						}

						var namingService = CurrentChat.Chat.Services.GetService<IChatNamingService>();
						if (namingService != null)
							await namingService.NameChatAsync();
					}
					catch (Exception ex)
					{
						Log.Error(ex, "Failed to rename chat with AI: {Error}", ex.Message);
					}
				}
			});
		}

		private void UnlockAvailableChat(AvailableChatViewModel available, Chat chat)
		{
			available.IsSelected = false;
			chat.PropertyChanged -= OnChatPropertyChanged;
		}

		private void LockAvailableChat(AvailableChatViewModel available, Chat chat)
		{
			available.IsSelected = true;
			chat.PropertyChanged += OnChatPropertyChanged;
		}

		private void OnChatPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			var chat = (Chat)sender!;
			_selectedAvailable!.Title = chat.Title;
			_selectedAvailable!.Topic = chat.Topic;
		}

		private void OpenConversation(AvailableChatViewModel available)
		{
			try
			{
				_currentChatScope?.Dispose();
				CurrentChat?.Dispose();
				ChatCleanup();
			}
			catch (Exception ex)
			{
				Log.Debug(ex, "Failed to cleanup old chat scope: {Error}", ex.Message);
			}

			_currentChatScope = ChatManager.OpenChatScope(available.Id);
			var chatServices = _currentChatScope.ServiceProvider;
			var chat = chatServices.GetRequiredService<Chat>();
			CurrentChat = new ChatViewModel(chat);
			LockAvailableChat(_selectedAvailable!, chat);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				try
				{
					_selectedAvailable?.IsSelected = false;
					SelectedAvailable = null;
					CurrentChat?.Dispose();
					ChatCleanup();
					_currentChatScope?.Dispose();
					_currentChatScope = null;
				}
				catch (Exception ex)
				{
					Log.Debug(ex, "Failed to dispose OpenedChatViewModel: {Error}", ex.Message);
				}
			}
		}

		// TODO: Удалить это говно внизу когда автор LiveMarkdown.Avalonia соизволит рассмотреть мой Pull Request
		// Утечки памяти вызваны Link.TagToLinkMap, который нихуя не чистится, я его там заменил на ConcurrentDict<WeakReference>
		// Ждем

		private static Dictionary<string, Link>? _tagToLinkMap;

		static OpenedChatViewModel()
		{
			try
			{
				var linkType = typeof(Link);
				var tagToLinkMapField = linkType.GetField("TagToLinkMap", BindingFlags.NonPublic | BindingFlags.Static);
				_tagToLinkMap = tagToLinkMapField?.GetValue(null) as Dictionary<string, Link>;

				if (_tagToLinkMap == null)
					Log.Error("Failed to get LiveMarkdown.Avalonia.Link.TagToLinkMap field.");
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to get LiveMarkdown.Avalonia.Link.TagToLinkMap field: {Error}", ex.Message);
			}
		}

		private void ChatCleanup()
		{
			_tagToLinkMap?.Clear();
		}
	}

	[ViewModelFor(typeof(ChatManagerView))]
	public class ChatManagerViewModel : ViewModelBase
	{
		public IChatManagementService ChatManager { get; }

		public ObservableCollection<AvailableChatViewModel> AvailableChats { get; } = [];

		public ObservableCollection<OpenedChatViewModel> OpenedChats { get; } = [];

		private OpenedChatViewModel? _selectedChat;
		public OpenedChatViewModel? SelectedChat
		{
			get => _selectedChat;
			set => SetProperty(ref _selectedChat, value);
		}

		public ICommand CreateConversationCommand { get; }

		public ChatManagerViewModel(IChatManagementService chatManager)
		{
			ChatManager = chatManager;
			CreateConversationCommand = new RelayCommand(CreateConversation);

			Initialize();
		}

		private void Initialize()
		{
			ChatManager.ClearEmptyChats();
			LoadConversations();
			CreateConversation();
		}

		private AvailableChatViewModel CreateAvailableChatViewModel(ChatInfo info)
		{
			AvailableChatViewModel newAvailableChatViewModel = null!;
			var deleteCommand = new RelayCommand(() =>
			{
				var openedChats = OpenedChats.Where(o => o.SelectedAvailable?.Id == info.Id).ToList();
				foreach (var openedChat in openedChats)
				{
					OpenedChats.Remove(openedChat);
					openedChat.Dispose();
				}
				ChatManager.DeleteChat(info.Id);
				AvailableChats.Remove(newAvailableChatViewModel);

				if (OpenedChats.Count == 0)
					CreateConversation();
			});
			newAvailableChatViewModel = AvailableChatViewModel.CreateFromInfo(info, deleteCommand);
			return newAvailableChatViewModel;
		}

		private void LoadConversations()
		{
			AvailableChats.Clear();
			foreach (var chat in ChatManager.GetChats().OrderByDescending(c => c.LastModifiedAt))
				AvailableChats.Add(CreateAvailableChatViewModel(chat));
		}

		private void CreateConversation()
		{
			var newChat = ChatManager.CreateChat(LocalizationManager.LocalizeStatic("new_chat"));
			var newAvailableChat = CreateAvailableChatViewModel(newChat);
			AvailableChats.Insert(0, newAvailableChat);

			OpenedChatViewModel newOpenedChat = null!;
			newOpenedChat = new OpenedChatViewModel
			{
				ChatManager = ChatManager,
				SelectedAvailable = newAvailableChat,
				CloseChatCommand = new RelayCommand(() =>
				{
					OpenedChats.Remove(newOpenedChat);
					newOpenedChat.Dispose();

					if (OpenedChats.Count == 0)
						CreateConversation();
				})
			};
			OpenedChats.Add(newOpenedChat);
			SelectedChat = newOpenedChat;
		}
	}
}
