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
		public IServiceProvider ServiceProvider { get; }
		public ConversationDatabase Database { get; }

		private IServiceScope? _currentChatScope;
		private ChatViewModel? _currentChat;
		public ChatViewModel? CurrentChat
		{
			get => _currentChat;
			private set => SetProperty(ref _currentChat, value);
		}

		public ObservableCollection<ConversationModel> Conversations { get; } = new();

		private ConversationModel? _selectedConversation;
		public ConversationModel? SelectedConversation
		{
			get => _selectedConversation;
			set
			{
				if (SetProperty(ref _selectedConversation, value) && value != null)
				{
					OpenConversation(value.Id);
				}
			}
		}

		public ICommand CreateConversationCommand { get; }

		public ChatManagerViewModel()
		{
			Database = new ConversationDatabase("conversations/chat.db");

			var serviceBuilder = new ServiceCollection();
			serviceBuilder.AddSingleton(Database);
			serviceBuilder.AddChatServices();

			ServiceProvider = serviceBuilder.BuildServiceProvider();

			EnsureDefaultConversation();
			LoadConversations();

			CreateConversationCommand = new RelayCommand(CreateConversation);

			SelectedConversation = Conversations.FirstOrDefault();
		}

		private void EnsureDefaultConversation()
		{
			if (Database.Conversations.FindById(1) == null)
			{
				Database.Conversations.Insert(new ConversationModel
				{
					Id = 1,
					SettingsProfile = ChatSettings.DefaultId,
					Title = "General"
				});
			}
		}

		private void LoadConversations()
		{
			Conversations.Clear();
			foreach (var conv in Database.Conversations.FindAll())
				Conversations.Add(conv);
		}

		private void OpenConversation(int id)
		{
			_currentChatScope?.Dispose();
			CurrentChat?.Chat.Dispose();

			_currentChatScope = ServiceProvider.CreateScope();

			var chat = _currentChatScope.ServiceProvider.GetRequiredService<LLM.Domain.Chat>();
			chat.ChatId = id;
			chat.AdditionalToolModules.Add(new AgenticToolModule());
			_currentChatScope.ServiceProvider.GetRequiredService<IChatStorageService>().Reload();

			CurrentChat = new ChatViewModel(chat);
		}

		private void CreateConversation()
		{
			var now = DateTime.Now;

			var model = new ConversationModel
			{
				SettingsProfile = ChatSettings.DefaultId,
				Title = $"Chat {now:HH:mm dd.MM}"
			};

			var id = Database.Conversations.Insert(model);
			model.Id = id;

			Conversations.Add(model);
			SelectedConversation = model;
		}
	}
}