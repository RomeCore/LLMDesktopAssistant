using LLMDesktopAssistant.Data;
using LLMDesktopAssistant.Data.ChatModels;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Services;
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
				Topic = model.Topic,
				CreatedAt = model.CreatedAt,
				LastModifiedAt = model.LastModifiedAt
			};
		}

		public IEnumerable<ChatInfo> GetChats()
		{
			return database.Chats.FindAll().Select(CreateChatInfo).ToList();
		}

		private IServiceScope OpenChatScope(ChatDatabase database, int chatId)
		{
			var scope = services.CreateScope();

			var chat = scope.ServiceProvider.GetRequiredService<Chat>();
			chat.ChatDatabase = database;
			chat.ChatId = chatId;

			// Ensure at least one default agent exists for this chat
			chat.Settings.Agents.EnsureDefaultAgent();

			var storage = scope.ServiceProvider.GetRequiredService<IChatStorageService>();
			storage.Reload();

			// Activate all chat-specific services
			var collection = scope.ServiceProvider.GetRequiredKeyedService<IServiceCollection>(ServiceRegistry.ChatServicesKey);
			foreach (var service in collection)
				if (!service.IsKeyedService && service.Lifetime != ServiceLifetime.Transient)
					scope.ServiceProvider.GetServices(service.ServiceType);

			return scope;
		}

		public IServiceScope OpenChatScope(int chatId)
		{
			return OpenChatScope(database, chatId);
		}

		public IServiceScope OpenMemoryChat()
		{
			var memoryDatabase = new ChatDatabase("Memory=true;");
			return OpenChatScope(memoryDatabase, 1);
		}

		public void ClearEmptyChats()
		{
			database.Chats.DeleteMany(c => c.LeafNodeId == -1 && c.RootNodeId == -1);
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

		public void DeleteChat(int chatId)
		{

		}
	}
}
