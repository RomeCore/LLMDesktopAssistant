using LLMDesktopAssistant.Data;
using LLMDesktopAssistant.Data.ChatModels;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace LLMDesktopAssistant.LLM.Services
{
	public class ChatManagementService(
		IServiceProvider services,
		ChatDatabase database
		) : IChatManagementService
	{
		private ChatInfo CreateChatInfo(ChatModel model)
		{
			return new ChatInfo
			{
				Id = model.Id,
				Title = model.Title,
				CreatedAt = model.CreatedAt,
				LastModifiedAt = model.LastModifiedAt
			};
		}

		public IEnumerable<ChatInfo> GetChats()
		{
			return database.Chats.FindAll().Select(CreateChatInfo).ToList();
		}

		public IServiceScope OpenChatScope(int chatId)
		{
			var scope = services.CreateScope();

			var chat = scope.ServiceProvider.GetRequiredService<Chat>();
			chat.ChatId = chatId;
			var storage = scope.ServiceProvider.GetRequiredService<IChatStorageService>();
			storage.Reload();

			return scope;
		}

		public void ClearEmptyAndTemporaryChats()
		{
			database.Chats.DeleteMany(c => (c.LeafNodeId == -1 && c.RootNodeId == -1) || c.IsTemporary);
		}

		public ChatInfo CreateChat(string title)
		{
			var model = new ChatModel
			{
				Title = title,
				CreatedAt = DateTime.Now,
				LastModifiedAt = DateTime.Now,
				RootNodeId = -1,
				LeafNodeId = -1
			};

			database.Chats.Insert(model);

			return CreateChatInfo(model);
		}
	}
}