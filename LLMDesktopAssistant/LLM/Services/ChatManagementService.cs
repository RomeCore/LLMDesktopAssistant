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
			var nodesToDelete = new List<int>();
			var nodesToCheck = database.MessageNodes
				.Find(m => m.IsRootNode && m.ParentId == chatId)
				.Select(m => m.Id)
				.ToList();

			while (nodesToCheck.Count > 0)
			{
				var lastElement = nodesToCheck[nodesToCheck.Count - 1];
				var childNodes = database.MessageNodes
					.Find(m => !m.IsRootNode && m.ParentId == lastElement)
					.Select(m => m.Id)
					.ToList();

				nodesToDelete.Add(lastElement);
				nodesToCheck.RemoveAt(nodesToCheck.Count - 1);
				nodesToCheck.AddRange(childNodes);
			}

			database.ChatContextTabViewModels.DeleteMany(c => c.ChatId == chatId);

			for (int i = 0; i < nodesToDelete.Count; i++)
			{
				DeleteNode(nodesToDelete[i]);
			}

			database.Chats.Delete(chatId);
		}

		private void DeleteNode(int nodeId)
		{
			var node = database.MessageNodes.FindById(nodeId);
			if (node == null) return;

			var message = database.Messages.FindById(node.MessageId);
			if (message == null)
			{
				database.MessageNodes.Delete(nodeId);
				return;
			}

			database.AdditionalMessageViewModels.DeleteMany(v => v.MessageId == message.Id);
			database.Attachments.DeleteMany(v => !v.IsParentToolCall && v.ParentId == message.Id);

			var toolCalls = database.ToolCalls.Find(m => m.MessageId == message.Id).ToList();
			foreach (var toolCall in toolCalls)
			{
				database.Attachments.DeleteMany(v => v.IsParentToolCall && v.ParentId == toolCall.Id);
			}
			database.ToolCalls.DeleteMany(m => m.MessageId == message.Id);

			database.Messages.Delete(message.Id);
			database.MessageNodes.Delete(nodeId);
		}
	}
}
