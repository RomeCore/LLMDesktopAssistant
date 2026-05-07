using CommunityToolkit.Mvvm.Input;
using LiveMarkdown.Avalonia;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Localization.Resources;
using Serilog;
using System.Collections.ObjectModel;
using System.Reflection;

namespace LLMDesktopAssistant.LLM
{
	[ViewModelFor(typeof(ChatManagerView))]
	public class ChatManagerViewModel : ViewModelBase
	{
		private IServiceScope? _currentChatScope;
		private ChatViewModel? _currentChat;
		public ChatViewModel? CurrentChat
		{
			get => _currentChat;
			private set => SetProperty(ref _currentChat, value);
		}

		public ObservableCollection<ChatInfo> Chats { get; } = [];

		private ChatInfo? _selectedChat;
		public ChatInfo? SelectedChat
		{
			get => _selectedChat;
			set
			{
				if (SetProperty(ref _selectedChat, value) && value != null)
				{
					OpenConversation(value.Id);
				}
			}
		}

		public ICommand CreateConversationCommand { get; }

		public ChatManagerViewModel()
		{
			CreateConversationCommand = new RelayCommand(CreateConversation);

			Initialize();
		}

		private void Initialize()
		{
			ChatServices.ManagementService.ClearEmptyAndTemporaryChats();
			LoadConversations();
			CreateConversation();
		}

		private void LoadConversations()
		{
			Chats.Clear();
			foreach (var chat in ChatServices.ManagementService.GetChats().OrderByDescending(c => c.LastModifiedAt))
				Chats.Add(chat);
		}

		private void OpenConversation(int id)
		{
			_currentChatScope?.Dispose();
			CurrentChat?.Dispose();
			ChatCleanup();

			_currentChatScope = ChatServices.ManagementService.OpenChatScope(id);
			var chatServices = _currentChatScope.ServiceProvider;
			var chat = chatServices.GetRequiredService<Chat>();
			var blazorStarter = chatServices.GetService<IChatBlazorUIStarter>();
			CurrentChat = new ChatViewModel(chat);

			if (blazorStarter != null)
			{
				blazorStarter.Start();
			}
		}

		private void CreateConversation()
		{
			var newChat = ChatServices.ManagementService.CreateChat(Locale.new_chat);
			Chats.Insert(0, newChat);
			SelectedChat = newChat;
		}

		// TODO: Удалить это говно внизу когда автор LiveMarkdown.Avalonia соизволит рассмотреть мой Pull Request
		// Утечки памяти вызваны Link.TagToLinkMap, который нихуя не чистится, я его там заменил на ConcurrentDict<WeakReference>
		// Ждем

		private static Dictionary<string, Link>? _tagToLinkMap;

		static ChatManagerViewModel()
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
}