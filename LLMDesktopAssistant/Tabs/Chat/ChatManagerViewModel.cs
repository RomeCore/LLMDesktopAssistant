using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Conversations;
using LLMDesktopAssistant.LLM.Conversations.Models;
using LLMDesktopAssistant.LLM.MVVM;
using LLMDesktopAssistant.MVVM;

namespace LLMDesktopAssistant.Tabs.Chat
{
	[ViewModelFor(typeof(ChatManagerView))]
	[TabTool("chat")]
	public class ChatManagerViewModel : ViewModelBase
	{
		public ConversationDatabase Database { get; }

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
					SystemInstructions = "You are a helpful assistant.",
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
			var manager = new ConversationManager(Database, id);
			CurrentChat = new ChatViewModel(manager);
		}

		private void CreateConversation()
		{
			var now = DateTime.Now;

			var model = new ConversationModel
			{
				SystemInstructions = "You are a helpful assistant.",
				Title = $"Chat {now:HH:mm dd.MM}"
			};

			var id = Database.Conversations.Insert(model);
			model.Id = id;

			Conversations.Add(model);
			SelectedConversation = model;
		}
	}
}