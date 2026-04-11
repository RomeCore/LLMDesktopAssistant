using LLMDesktopAssistant.LLM.Data;
using LLMDesktopAssistant.LLM.Data.Models;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.ToolModules;
using Microsoft.Extensions.DependencyInjection;

namespace LLMDesktopAssistant.LLM.Services
{
	public class ChatManagementService(
		IServiceProvider services,
		ConversationDatabase database
		) : IChatManagementService
	{
		private ChatInfo CreateChatInfo(ConversationModel model)
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
			return database.Conversations.FindAll().Select(CreateChatInfo).ToList();
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
			database.Conversations.DeleteMany(c => (c.LeafNodeId == -1 && c.RootNodeId == -1) || c.IsTemporary);
		}

		public ChatInfo CreateChat(string title)
		{
			var model = new ConversationModel
			{
				Title = title,
				CreatedAt = DateTime.Now,
				LastModifiedAt = DateTime.Now,
				RootNodeId = -1,
				LeafNodeId = -1
			};

			database.Conversations.Insert(model);

			return CreateChatInfo(model);
		}
	}
}