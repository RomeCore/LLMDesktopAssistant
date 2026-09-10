using LLMDesktopAssistant.Data;
using LLMDesktopAssistant.Data.ChatModels;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Storage;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// The main chat storage service. Responsible for the message tree (nodes), for the
	/// <see cref="Chat.Messages"/> collection and for the lifecycle of per-message synchronizers.
	/// The actual message<->database synchronization logic lives in
	/// <see cref="MessageDatabaseSynchronizer"/> (per message) and
	/// <see cref="ChatDatabaseSynchronizer"/> (per chat).
	/// </summary>
	[ChatService(typeof(IChatStorageService))]
	public class ChatStorageService(
		Chat chat,
		IChatCreationConfig config,
		IChatSettingsService chatSettings
	) : Disposable, IChatStorageService
	{
		readonly int chatId = config.ChatId;
		readonly ChatDatabase database = config.Database;

		ChatDatabaseSynchronizer? _chatSync;

		/// <summary>
		/// Synchronizers of the messages that are currently loaded into <see cref="Chat.Messages"/>,
		/// keyed by the domain message object.
		/// </summary>
		readonly Dictionary<ChatMessage, MessageDatabaseSynchronizer> _messageSyncs = [];

		// =====================================================================================
		// Loading / unloading
		// =====================================================================================

		public void Reload()
		{
			_chatSync?.Dispose();
			foreach (var message in chat.Messages.Select(b => b.Message).ToList())
				UnloadMessage(message);

			// Loads/creates the chat model, applies title/topic/settings and attaches UserInputState.
			_chatSync = new ChatDatabaseSynchronizer(database, chat, chatSettings);

			var messages = new List<BranchedMessage>();
			var currentNodeId = _chatSync.Model.RootNodeId;
			while (currentNodeId != -1)
			{
				var nodeModel = database.MessageNodes.FindById(currentNodeId);
				messages.Add(LoadMessageFromNode(nodeModel, messages.Count));
				currentNodeId = nodeModel.SelectedNodeId;
			}

			chat.Messages.Reset(messages);
		}

		/// <summary>
		/// Loads a message from the database by its node, creates its synchronizer and
		/// registers it in <see cref="_messageSyncs"/>.
		/// </summary>
		private BranchedMessage LoadMessageFromNode(MessageNodeModel nodeModel, int messageIndex)
		{
			var messageModel = database.Messages.FindById(nodeModel.MessageId);
			var sync = MessageDatabaseSynchronizer.FromModel(database, messageModel);
			_messageSyncs[sync.Target] = sync;
			return CreateBranchedMessage(nodeModel, sync.Target, messageIndex);
		}

		/// <summary>
		/// Removes a message from memory: disposes its synchronizer (the rows stay in the database)
		/// and disposes the domain object itself.
		/// </summary>
		private void UnloadMessage(ChatMessage message)
		{
			if (_messageSyncs.Remove(message, out var sync))
				sync.Dispose();
			message.Dispose();
		}

		// =====================================================================================
		// Tree operations (nodes stay here)
		// =====================================================================================

		public void AppendMessage(ChatMessage chatMessage)
		{
			if (_chatSync is null)
				throw new InvalidOperationException("Chat is not loaded.");
			if (!database.Database.BeginTrans())
				throw new InvalidOperationException("Failed to begin transaction.");

			var sync = MessageDatabaseSynchronizer.FromTarget(database, chatMessage);
			int messageId = sync.ModelId;
			MessageNodeModel nodeModel;

			try
			{
				var chatModel = _chatSync.Model;
				if (chatModel.RootNodeId == -1)
				{
					// Add root node
					int nodeId = database.MessageNodes.Insert(nodeModel = new MessageNodeModel
					{
						IsRootNode = true,
						ParentId = chatId,
						MessageId = messageId
					});
					chatModel.RootNodeId = nodeId;
					chatModel.LeafNodeId = nodeId;
					chatModel.LastModifiedAt = DateTime.Now;
				}
				else
				{
					var leafNode = database.MessageNodes.FindById(chatModel.LeafNodeId);
					int nodeId = database.MessageNodes.Insert(nodeModel = new MessageNodeModel
					{
						IsRootNode = false,
						ParentId = leafNode.Id,
						MessageId = messageId
					});

					leafNode.SelectedNodeId = nodeId;
					chatModel.LeafNodeId = nodeId;
					chatModel.LastModifiedAt = DateTime.Now;
					database.MessageNodes.Update(leafNode);
				}
				_chatSync.UpdateModel();

				if (!database.Database.Commit())
				{
					database.Database.Rollback();
					throw new InvalidOperationException("Failed to commit transaction.");
				}
			}
			catch
			{
				sync.Dispose();
				throw;
			}

			_messageSyncs[chatMessage] = sync;
			chat.Messages.Add(CreateBranchedMessage(nodeModel, chatMessage, chat.Messages.Count));
		}

		public void PlaceNewBranch(int messageIndex)
		{
			if (_chatSync is null)
				throw new InvalidOperationException("Chat is not loaded.");
			if (messageIndex < 0 || messageIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(messageIndex));

			if (!database.Database.BeginTrans())
				throw new InvalidOperationException("Failed to begin transaction.");

			var chatModel = _chatSync.Model;
			int newLeafNodeId = -1;
			int currentNodeId = chatModel.RootNodeId;
			int currentIndex = 0;

			while (currentIndex < messageIndex)
			{
				var node = database.MessageNodes.FindById(currentNodeId);
				newLeafNodeId = currentNodeId;
				currentNodeId = node.SelectedNodeId;
				currentIndex++;
			}

			if (newLeafNodeId != -1)
			{
				var leafNode = database.MessageNodes.FindById(newLeafNodeId);
				leafNode.SelectedNodeId = -1;
				database.MessageNodes.Update(leafNode);
			}

			if (newLeafNodeId == -1)
				chatModel.RootNodeId = -1;

			chatModel.LeafNodeId = newLeafNodeId;
			chatModel.LastModifiedAt = DateTime.Now;
			_chatSync.UpdateModel();

			if (!database.Database.Commit())
				throw new InvalidOperationException("Failed to commit transaction.");

			for (int i = messageIndex; i < chat.Messages.Count; i++)
				UnloadMessage(chat.Messages[i].Message);
			chat.Messages.RemoveRange(messageIndex, chat.Messages.Count - messageIndex);
		}

		public void SwitchBranch(int messageIndex, int newBranchIndex)
		{
			if (_chatSync is null)
				throw new InvalidOperationException("Chat is not loaded.");
			if (messageIndex < 0 || messageIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(messageIndex));

			if (!database.Database.BeginTrans())
				throw new InvalidOperationException("Failed to begin transaction.");

			var chatModel = _chatSync.Model;
			int currentNodeId = chatModel.RootNodeId;
			int currentIndex = 0;

			while (currentIndex < messageIndex)
			{
				var node = database.MessageNodes.FindById(currentNodeId);
				currentNodeId = node.SelectedNodeId;
				currentIndex++;
			}

			var currentNode = database.MessageNodes.FindById(currentNodeId);

			var siblings = database.MessageNodes
				.Find(n => n.ParentId == currentNode.ParentId &&
						   n.IsRootNode == currentNode.IsRootNode)
				.OrderBy(n => n.Id)
				.ToList();

			if (newBranchIndex < 0 || newBranchIndex >= siblings.Count)
				throw new ArgumentOutOfRangeException(nameof(newBranchIndex));

			var selectedNode = siblings[newBranchIndex];

			if (currentNode.IsRootNode)
			{
				chatModel.RootNodeId = selectedNode.Id;
			}
			else
			{
				var parent = database.MessageNodes.FindById(currentNode.ParentId);
				parent.SelectedNodeId = selectedNode.Id;
				database.MessageNodes.Update(parent);
			}

			List<BranchedMessage> subsequentMessages = [];

			currentNodeId = selectedNode.Id;
			int leafId = currentNodeId;
			while (currentNodeId != -1)
			{
				var node = database.MessageNodes.FindById(currentNodeId);
				subsequentMessages.Add(LoadMessageFromNode(node, messageIndex + subsequentMessages.Count));
				leafId = currentNodeId;
				currentNodeId = node.SelectedNodeId;
			}

			chatModel.LeafNodeId = leafId;
			chatModel.LastModifiedAt = DateTime.Now;
			_chatSync.UpdateModel();

			if (!database.Database.Commit())
				throw new InvalidOperationException("Failed to commit transaction.");

			for (int i = messageIndex; i < chat.Messages.Count; i++)
				UnloadMessage(chat.Messages[i].Message);
			chat.Messages.ReplaceRange(messageIndex, chat.Messages.Count - messageIndex, subsequentMessages);
		}

		public void EditMessage(int messageIndex, ChatMessage newMessage)
		{
			if (_chatSync is null)
				throw new InvalidOperationException("Chat is not loaded.");
			if (messageIndex < 0 || messageIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(messageIndex));

			if (!database.Database.BeginTrans())
				throw new InvalidOperationException("Failed to begin transaction.");

			var sync = MessageDatabaseSynchronizer.FromTarget(database, newMessage);
			int messageId = sync.ModelId;
			MessageNodeModel newNode;

			try
			{
				var chatModel = _chatSync.Model;
				int currentNodeId = chatModel.RootNodeId;
				int currentIndex = 0;

				while (currentIndex < messageIndex)
				{
					var node = database.MessageNodes.FindById(currentNodeId);
					currentNodeId = node.SelectedNodeId;
					currentIndex++;
				}

				var currentNode = database.MessageNodes.FindById(currentNodeId);

				newNode = new MessageNodeModel
				{
					IsRootNode = currentNode.IsRootNode,
					ParentId = currentNode.ParentId,
					MessageId = messageId
				};

				int newNodeId = database.MessageNodes.Insert(newNode);

				if (currentNode.IsRootNode)
				{
					chatModel.RootNodeId = newNodeId;
				}
				else
				{
					var parent = database.MessageNodes.FindById(currentNode.ParentId);
					parent.SelectedNodeId = newNodeId;
					database.MessageNodes.Update(parent);
				}

				chatModel.LeafNodeId = newNodeId;
				chatModel.LastModifiedAt = DateTime.Now;
				_chatSync.UpdateModel();

				if (!database.Database.Commit())
				{
					database.Database.Rollback();
					throw new InvalidOperationException("Failed to commit transaction.");
				}
			}
			catch
			{
				sync.Dispose();
				throw;
			}

			_messageSyncs[newMessage] = sync;
			for (int i = messageIndex; i < chat.Messages.Count; i++)
				UnloadMessage(chat.Messages[i].Message);
			chat.Messages.ReplaceRange(messageIndex, chat.Messages.Count - messageIndex, [CreateBranchedMessage(newNode, newMessage, messageIndex)]);
		}

		public void DeleteMessageWithDescendants(int messageIndex)
		{
			if (_chatSync is null)
				throw new InvalidOperationException("Chat is not loaded.");
			if (messageIndex < 0 || messageIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(messageIndex));

			if (!database.Database.BeginTrans())
				throw new InvalidOperationException("Failed to begin transaction.");

			var chatModel = _chatSync.Model;
			int currentNodeId = chatModel.RootNodeId;
			int currentIndex = 0;

			while (currentIndex < messageIndex)
			{
				var node = database.MessageNodes.FindById(currentNodeId);
				currentNodeId = node.SelectedNodeId;
				currentIndex++;
			}

			var currentNode = database.MessageNodes.FindById(currentNodeId);
			var siblings = database.MessageNodes
				.Find(n => n.ParentId == currentNode.ParentId &&
						   n.IsRootNode == currentNode.IsRootNode)
				.OrderBy(n => n.Id)
				.ToList();
			int siblingIndex = siblings.FindIndex(n => n.Id == currentNodeId);

			// Delete all descendants (all child branches, not only the selected one)
			var nodesToDelete = new List<int>();
			List<int> nodesToCheck = [currentNodeId];

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

			for (int i = 0; i < nodesToDelete.Count; i++)
			{
				var node = database.MessageNodes.FindById(nodesToDelete[i]);
				if (node == null) continue;

				var messageModel = database.Messages.FindById(node.MessageId);
				if (messageModel != null)
					MessageDatabaseSynchronizer.DeleteFromDatabase(database, messageModel.Id);

				database.MessageNodes.Delete(node.Id);
			}

			// Select another sibling
			List<BranchedMessage> subsequentMessages = [];
			siblings = database.MessageNodes
				.Find(n => n.ParentId == currentNode.ParentId &&
						   n.IsRootNode == currentNode.IsRootNode)
				.OrderBy(n => n.Id)
				.ToList();
			if (siblings.Count > 0)
			{
				siblingIndex--;
				if (siblingIndex < 0)
					siblingIndex = 0;
				var selectedNode = siblings[siblingIndex];

				if (currentNode.IsRootNode)
				{
					chatModel.RootNodeId = selectedNode.Id;
				}
				else
				{
					var parent = database.MessageNodes.FindById(currentNode.ParentId);
					parent.SelectedNodeId = selectedNode.Id;
					database.MessageNodes.Update(parent);
				}

				currentNodeId = selectedNode.Id;
				int leafId = currentNodeId;
				while (currentNodeId != -1)
				{
					var node = database.MessageNodes.FindById(currentNodeId);
					subsequentMessages.Add(LoadMessageFromNode(node, messageIndex + subsequentMessages.Count));
					leafId = currentNodeId;
					currentNodeId = node.SelectedNodeId;
				}

				chatModel.LeafNodeId = leafId;
				if (currentNode.IsRootNode)
					chatModel.RootNodeId = selectedNode.Id;
				chatModel.LastModifiedAt = DateTime.Now;
			}
			else
			{
				chatModel.LeafNodeId = currentNode.IsRootNode ? -1 : currentNode.ParentId;
				if (currentNode.IsRootNode)
					chatModel.RootNodeId = -1;
				chatModel.LastModifiedAt = DateTime.Now;
			}
			_chatSync.UpdateModel();

			if (!database.Database.Commit())
				throw new InvalidOperationException("Failed to commit transaction.");

			for (int i = messageIndex; i < chat.Messages.Count; i++)
				UnloadMessage(chat.Messages[i].Message);
			chat.Messages.ReplaceRange(messageIndex, chat.Messages.Count - messageIndex, subsequentMessages);
		}

		// =====================================================================================
		// Helpers
		// =====================================================================================

		private BranchedMessage CreateBranchedMessage(MessageNodeModel nodeModel, ChatMessage message, int messageIndex)
		{
			var sameOrderNodes = database.MessageNodes.Find(n => n.ParentId == nodeModel.ParentId &&
				n.IsRootNode == nodeModel.IsRootNode).Select(n => n.Id).OrderBy(i => i).ToList();
			int selectedNode = sameOrderNodes.IndexOf(nodeModel.Id);

			return new BranchedMessage
			{
				Message = message,
				MessageId = nodeModel.MessageId,
				MessageIndex = messageIndex,
				AvailableBranchesCount = sameOrderNodes.Count,
				SelectedBranchIndex = selectedNode
			};
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (!disposing) return;

			_chatSync?.Dispose();
			_chatSync = null;

			foreach (var message in chat.Messages.Select(b => b.Message).ToList())
				UnloadMessage(message);
			chat.Messages.Clear();
		}
	}
}
