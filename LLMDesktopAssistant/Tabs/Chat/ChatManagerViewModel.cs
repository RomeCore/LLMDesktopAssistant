using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Data;
using LLMDesktopAssistant.LLM.Data.Models;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Localization.Resources;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.ToolModules;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace LLMDesktopAssistant.Tabs.Chat
{
	[ViewModelFor(typeof(ChatManagerView))]
	[TabTool("chat", Icon = PackIconKind.Message)]
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
			ChatServices.ManagementService.ClearEmptyChats();
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
			_currentChatScope = ChatServices.ManagementService.OpenChatScope(id);
			var chat = _currentChatScope.ServiceProvider.GetRequiredService<LLM.Domain.Chat>();
			CurrentChat = new ChatViewModel(chat);
		}

		private void CreateConversation()
		{
			var newChat = ChatServices.ManagementService.CreateChat(Locale.new_chat);
			Chats.Insert(0, newChat);
			SelectedChat = newChat;
		}
	}
}