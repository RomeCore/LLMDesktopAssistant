using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.Data;
using LLMDesktopAssistant.Data.ChatModels;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM.ContextTabs;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Tasks;

namespace LLMDesktopAssistant.LLM.Services
{
	[ChatService(typeof(IChatStorageService))]
	public class ChatStorageService(
		Chat chat
	) : Disposable, IChatStorageService
	{
		readonly int chatId = chat.ChatId;
		readonly ChatDatabase database = chat.ChatDatabase;
		Action? _mainUnsubscriber;
		readonly MultiValueDictionary<ChatMessage, Action> _unsubscribers = [];

		public void Reload()
		{
			_mainUnsubscriber?.Invoke();
			for (int i = 0; i < chat.Messages.Count; i++)
				Unsubscribe(chat.Messages[i].Message);

			var messages = new List<BranchedMessage>();

			var chatModel = database.Chats.FindById(chatId);
			if (chatModel == null)
			{
				chatModel = new ChatModel
				{
					Id = chatId,
					Title = chat.Title,
					LeafNodeId = -1,
					RootNodeId = -1,
					SettingsProfile = ChatSettings.DefaultId,
					CreatedAt = DateTime.Now,
					LastModifiedAt = DateTime.Now
				};
				database.Chats.Insert(chatModel);
			}

			chat.Title = chatModel.Title;
			chat.Topic = chatModel.Topic;
			chat.Settings = SettingsManager.Get<ChatSettings>(chatModel.SettingsProfile);

			var currentNodeId = chatModel.RootNodeId;
			while (currentNodeId != -1)
			{
				var nodeModel = database.MessageNodes.FindById(currentNodeId);
				var messageModel = database.Messages.FindById(nodeModel.MessageId);
				var chatMessage = CreateChatMessage(messageModel);
				messages.Add(CreateBranchedMessage(nodeModel, chatMessage, messages.Count));

				currentNodeId = nodeModel.SelectedNodeId;
			}

			var contextTabViewModels = database.ChatContextTabViewModels
				.Find(avm => avm.ChatId == chat.ChatId)
				.OrderBy(avm => avm.Id);

			foreach (var contextTabViewDataModel in contextTabViewModels)
			{
				SubscribeContextTabViewModel(contextTabViewDataModel.ViewModel, contextTabViewDataModel);
				chat.ContextTabs.Add(contextTabViewDataModel.ViewModel);
			}

			_mainUnsubscriber?.Invoke();
			_mainUnsubscriber = null;
			void ChatPropertyChanged(object? s, PropertyChangedEventArgs e)
			{
				chatModel = database.Chats.FindById(chatId);

				chatModel.Title = chat.Title;
				chatModel.Topic = chat.Topic;
				chatModel.SettingsProfile = chat.Settings.Id;

				database.Chats.Update(chatModel);
			}
			chat.PropertyChanged += ChatPropertyChanged;
			chat.ContextTabs.CollectionChanged += ChatContextTabsCollectionChanged;
			_mainUnsubscriber += () =>
			{
				chat.PropertyChanged -= ChatPropertyChanged;
				chat.ContextTabs.CollectionChanged -= ChatContextTabsCollectionChanged;
			};

			chat.Messages.Reset(messages);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			_mainUnsubscriber?.Invoke();
			for (int i = 0; i < chat.Messages.Count; i++)
				Unsubscribe(chat.Messages[i].Message);
			chat.Messages.Clear();
		}

		private void ChatContextTabsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
				foreach (ChatContextTabViewModel oldVm in e.OldItems)
					database.AdditionalMessageViewModels.DeleteMany(avm => avm.ViewModel.Guid == oldVm.Guid);

			if (e.NewItems != null)
				foreach (ChatContextTabViewModel newVm in e.NewItems)
					CreateAndInsertContextTabViewModelAndSubscribe(newVm);
		}

		private ChatContextTabViewDataModel CreateAndInsertContextTabViewModel(ChatContextTabViewModel viewModel)
		{
			var contextTabViewDataModel = new ChatContextTabViewDataModel
			{
				ChatId = chat.ChatId,
				ViewModel = viewModel
			};
			database.ChatContextTabViewModels.Insert(contextTabViewDataModel);
			return contextTabViewDataModel;
		}

		private void SubscribeContextTabViewModel(ChatContextTabViewModel viewModel, ChatContextTabViewDataModel? model)
		{
			bool prevTemporary = viewModel.IsTemporary;
			void OnContextTabViewModelPropertyChanged()
			{
				if (prevTemporary != viewModel.IsTemporary)
				{
					prevTemporary = viewModel.IsTemporary;

					if (prevTemporary) // Became persistent
					{
						model = CreateAndInsertContextTabViewModel(viewModel);
					}
					else // Became temporary
					{
						database.ChatContextTabViewModels.DeleteMany(avm => avm.ViewModel.Guid == viewModel.Guid);
					}
				}
				else if (model != null)
					database.ChatContextTabViewModels.Update(model);
			}
			var changeTracker = new ChangeTracker(viewModel, OnContextTabViewModelPropertyChanged);
			_mainUnsubscriber += changeTracker.Dispose;
		}

		private void CreateAndInsertContextTabViewModelAndSubscribe(ChatContextTabViewModel viewModel)
		{
			var contextTabViewDataModel = viewModel.IsTemporary
				? null
				: CreateAndInsertContextTabViewModel(viewModel);

			SubscribeContextTabViewModel(viewModel, contextTabViewDataModel);
		}

		public void AppendMessage(ChatMessage chatMessage)
		{
			if (!database.Database.BeginTrans())
				throw new InvalidOperationException("Failed to begin transaction.");

			var conversation = database.Chats.FindById(chatId);
			int messageId = CreateAndInsertMessageModel(chatMessage).Id;
			MessageNodeModel nodeModel;

			if (conversation.RootNodeId == -1)
			{
				// Add root node
				int nodeId = database.MessageNodes.Insert(nodeModel = new MessageNodeModel
				{
					IsRootNode = true,
					ParentId = chatId,
					MessageId = messageId
				});
				conversation.RootNodeId = nodeId;
				conversation.LeafNodeId = nodeId;
				conversation.LastModifiedAt = DateTime.Now;
				database.Chats.Update(conversation);
			}
			else
			{
				var leafNode = database.MessageNodes.FindById(conversation.LeafNodeId);
				int nodeId = database.MessageNodes.Insert(nodeModel = new MessageNodeModel
				{
					IsRootNode = false,
					ParentId = leafNode.Id,
					MessageId = messageId
				});

				leafNode.SelectedNodeId = nodeId;
				conversation.LeafNodeId = nodeId;
				conversation.LastModifiedAt = DateTime.Now;
				database.MessageNodes.Update(leafNode);
				database.Chats.Update(conversation);
			}

			if (!database.Database.Commit())
				throw new InvalidOperationException("Failed to commit transaction.");

			chat.Messages.Add(CreateBranchedMessage(nodeModel, chatMessage, chat.Messages.Count));
		}

		public void PlaceNewBranch(int messageIndex)
		{
			if (messageIndex < 0 || messageIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(messageIndex));

			if (!database.Database.BeginTrans())
				throw new InvalidOperationException("Failed to begin transaction.");

			var conversation = database.Chats.FindById(chatId);
			int newLeafNodeId = -1;
			int currentNodeId = conversation.RootNodeId;
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
				conversation.RootNodeId = -1;

			conversation.LeafNodeId = newLeafNodeId;
			conversation.LastModifiedAt = DateTime.Now;
			database.Chats.Update(conversation);

			if (!database.Database.Commit())
				throw new InvalidOperationException("Failed to commit transaction.");

			for (int i = messageIndex; i < chat.Messages.Count; i++)
				Unsubscribe(chat.Messages[i].Message);
			chat.Messages.RemoveRange(messageIndex, chat.Messages.Count - messageIndex);
		}

		public void SwitchBranch(int messageIndex, int newBranchIndex)
		{
			if (messageIndex < 0 || messageIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(messageIndex));

			if (!database.Database.BeginTrans())
				throw new InvalidOperationException("Failed to begin transaction.");

			var conversation = database.Chats.FindById(chatId);
			int currentNodeId = conversation.RootNodeId;
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
				conversation.RootNodeId = selectedNode.Id;
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
				var messageModel = database.Messages.FindById(node.MessageId);
				subsequentMessages.Add(CreateBranchedMessage(node, CreateChatMessage(messageModel), messageIndex + subsequentMessages.Count));

				leafId = currentNodeId;
				currentNodeId = node.SelectedNodeId;
			}

			conversation.LeafNodeId = leafId;
			conversation.LastModifiedAt = DateTime.Now;
			database.Chats.Update(conversation);

			if (!database.Database.Commit())
				throw new InvalidOperationException("Failed to commit transaction.");

			for (int i = messageIndex; i < chat.Messages.Count; i++)
				Unsubscribe(chat.Messages[i].Message);
			chat.Messages.ReplaceRange(messageIndex, chat.Messages.Count - messageIndex, subsequentMessages);
		}

		public void EditMessage(int messageIndex, ChatMessage newMessage)
		{
			if (messageIndex < 0 || messageIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(messageIndex));

			if (!database.Database.BeginTrans())
				throw new InvalidOperationException("Failed to begin transaction.");

			var conversation = database.Chats.FindById(chatId);
			int currentNodeId = conversation.RootNodeId;
			int currentIndex = 0;

			while (currentIndex < messageIndex)
			{
				var node = database.MessageNodes.FindById(currentNodeId);
				currentNodeId = node.SelectedNodeId;
				currentIndex++;
			}

			var currentNode = database.MessageNodes.FindById(currentNodeId);
			int messageId = CreateAndInsertMessageModel(newMessage).Id;

			var newNode = new MessageNodeModel
			{
				IsRootNode = currentNode.IsRootNode,
				ParentId = currentNode.ParentId,
				MessageId = messageId
			};

			int newNodeId = database.MessageNodes.Insert(newNode);

			if (currentNode.IsRootNode)
			{
				conversation.RootNodeId = newNodeId;
			}
			else
			{
				var parent = database.MessageNodes.FindById(currentNode.ParentId);
				parent.SelectedNodeId = newNodeId;
				database.MessageNodes.Update(parent);
			}

			conversation.LeafNodeId = newNodeId;
			conversation.LastModifiedAt = DateTime.Now;
			database.Chats.Update(conversation);

			if (!database.Database.Commit())
				throw new InvalidOperationException("Failed to commit transaction.");

			for (int i = messageIndex; i < chat.Messages.Count; i++)
				Unsubscribe(chat.Messages[i].Message);
			chat.Messages.ReplaceRange(messageIndex, chat.Messages.Count - messageIndex, [CreateBranchedMessage(newNode, newMessage, messageIndex)]);
		}

		public void DeleteMessageWithDescendants(int messageIndex)
		{
			if (messageIndex < 0 || messageIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(messageIndex));

			if (!database.Database.BeginTrans())
				throw new InvalidOperationException("Failed to begin transaction.");

			var conversation = database.Chats.FindById(chatId);
			int currentNodeId = conversation.RootNodeId;
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

			// Delete all descendants
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
				DeleteNode(nodesToDelete[i]);
			}

			// Select another sibling
			List<BranchedMessage> subsequentMessages = [];
			if (siblings.Count > 1)
			{
				siblingIndex--;
				if (siblingIndex < 0)
					siblingIndex = 0;
				var selectedNode = siblings[siblingIndex];

				if (currentNode.IsRootNode)
				{
					conversation.RootNodeId = selectedNode.Id;
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
					var messageModel = database.Messages.FindById(node.MessageId);
					subsequentMessages.Add(CreateBranchedMessage(node, CreateChatMessage(messageModel), messageIndex + subsequentMessages.Count));

					leafId = currentNodeId;
					currentNodeId = node.SelectedNodeId;
				}

				conversation.LeafNodeId = leafId;
				if (currentNode.IsRootNode)
					conversation.RootNodeId = selectedNode.Id;
				conversation.LastModifiedAt = DateTime.Now;
				database.Chats.Update(conversation);

				if (!database.Database.Commit())
					throw new InvalidOperationException("Failed to commit transaction.");
			}
			else
			{
				conversation.LeafNodeId = currentNode.IsRootNode ? -1 : currentNode.ParentId;
				if (currentNode.IsRootNode)
					conversation.RootNodeId = -1;
				conversation.LastModifiedAt = DateTime.Now;
				database.Chats.Update(conversation);

				if (!database.Database.Commit())
					throw new InvalidOperationException("Failed to commit transaction.");
			}

			for (int i = messageIndex; i < chat.Messages.Count; i++)
				Unsubscribe(chat.Messages[i].Message);
			chat.Messages.ReplaceRange(messageIndex, chat.Messages.Count - messageIndex, subsequentMessages);
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

		private ToolCallModel CreateAndInsertToolCallModel(ToolCall toolCall, MessageModel model)
		{
			var toolCallModel = new ToolCallModel
			{
				MessageId = model.Id,
				ToolCallId = toolCall.Id,
				ToolName = toolCall.ToolName,
				Title = toolCall.Title,
				FunctionArguments = toolCall.Arguments,
				Status = toolCall.Status switch
				{
					ToolStatus.None => ToolStatusModel.NotExecuted,
					ToolStatus.Executing => ToolStatusModel.ExecutionBegin,
					ToolStatus.Success => ToolStatusModel.Success,
					ToolStatus.Error => ToolStatusModel.Error,
					ToolStatus.Cancelled => ToolStatusModel.Cancelled,
					_ => ToolStatusModel.NotExecuted,
				},
				StatusIcon = toolCall.StatusIcon,
				StatusTitle = toolCall.StatusTitle,
				ExpectedBehaviour = toolCall.ExpectedBehaviour,
				ResultContent = toolCall.ResultContent,
				UseMarkdown = toolCall.UseMarkdown,
				StructuredResult = toolCall.StructuredResult?.ToJsonString()
			};

			database.ToolCalls.Insert(toolCallModel);

			foreach (var attachment in toolCall.Attachments)
			{
				var attachmentModel = new AttachmentModel
				{
					IsParentToolCall = true,
					ParentId = toolCallModel.Id,
					Attachment = attachment
				};
				database.Attachments.Insert(attachmentModel);
			}

			return toolCallModel;
		}

		private void SubscribeToolCall(ToolCall toolCall, AssistantMessage assistantMessage, ToolCallModel model)
		{
			async void OnToolCallPropertyChanged(object? sender, PropertyChangedEventArgs e)
			{
				model.Status = toolCall.Status switch
				{
					ToolStatus.None => ToolStatusModel.NotExecuted,
					ToolStatus.Executing => ToolStatusModel.ExecutionBegin,
					ToolStatus.Success => ToolStatusModel.Success,
					ToolStatus.Error => ToolStatusModel.Error,
					ToolStatus.Cancelled => ToolStatusModel.Cancelled,
					_ => ToolStatusModel.NotExecuted,
				};
				model.FunctionArguments = toolCall.Arguments;
				model.StatusIcon = toolCall.StatusIcon;
				model.StatusTitle = toolCall.StatusTitle;
				model.ExpectedBehaviour = toolCall.ExpectedBehaviour;
				model.ResultContent = toolCall.ResultContent;
				model.UseMarkdown = toolCall.UseMarkdown;
				model.StructuredResult = toolCall.StructuredResult?.ToJsonString();

				database.ToolCalls.Update(model);
			}
			toolCall.PropertyChanged += OnToolCallPropertyChanged;

			void AttachmentsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
			{
				if (e.OldItems != null)
					foreach (Attachment oldAttachment in e.OldItems)
						database.Attachments.DeleteMany(avm => avm.Attachment.Guid == oldAttachment.Guid);

				if (e.NewItems != null)
					foreach (Attachment newAttachment in e.NewItems)
					{
						var attachmentModel = new AttachmentModel
						{
							IsParentToolCall = true,
							ParentId = model.Id,
							Attachment = newAttachment
						};
						database.Attachments.Insert(attachmentModel);
					}
			}
			toolCall.Attachments.CollectionChanged += AttachmentsCollectionChanged;

			_unsubscribers.Add(assistantMessage, () =>
			{
				toolCall.PropertyChanged -= OnToolCallPropertyChanged;
				toolCall.Attachments.CollectionChanged -= AttachmentsCollectionChanged;
			});
		}

		private void CreateAndInsertToolCallModelAndSubscribe(ToolCall toolCall, AssistantMessage assistantMessage, MessageModel model)
		{
			var toolCallModel = CreateAndInsertToolCallModel(toolCall, model);

			SubscribeToolCall(toolCall, assistantMessage, toolCallModel);
		}

		private AdditionalMessageViewDataModel CreateAndInsertAdditionalViewModel(AdditionalMessageViewModel viewModel,
			MessageModel model)
		{
			var additionalViewDataModel = new AdditionalMessageViewDataModel
			{
				MessageId = model.Id,
				ViewModel = viewModel
			};
			database.AdditionalMessageViewModels.Insert(additionalViewDataModel);
			return additionalViewDataModel;
		}

		private void SubscribeAdditionalViewModel(AdditionalMessageViewModel viewModel, ChatMessage message,
			MessageModel messageModel, AdditionalMessageViewDataModel? model)
		{
			bool prevTemporary = viewModel.IsTemporary;
			void OnAdditionalViewModelPropertyChanged()
			{
				if (prevTemporary != viewModel.IsTemporary)
				{
					prevTemporary = viewModel.IsTemporary;

					if (prevTemporary) // Became persistent
					{
						model = CreateAndInsertAdditionalViewModel(viewModel, messageModel);
					}
					else // Became temporary
					{
						database.AdditionalMessageViewModels.DeleteMany(avm => avm.ViewModel.Guid == viewModel.Guid);
					}
				}
				else if (model != null)
					database.AdditionalMessageViewModels.Update(model);
			}
			var changeTracker = new ChangeTracker(viewModel, OnAdditionalViewModelPropertyChanged);
			_unsubscribers.Add(message, changeTracker.Dispose);
		}

		private void CreateAndInsertAdditionalViewModelAndSubscribe(AdditionalMessageViewModel viewModel,
			ChatMessage message, MessageModel messageModel)
		{
			var additionalViewDataModel = viewModel.IsTemporary
				? null
				: CreateAndInsertAdditionalViewModel(viewModel, messageModel);

			SubscribeAdditionalViewModel(viewModel, message, messageModel, additionalViewDataModel);
		}

		private void SubscribeMessage(ChatMessage message, MessageModel model)
		{
			if (message is UserMessage userMessage)
			{
				async void MessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
				{
					model.Content = userMessage.Content;

					database.Messages.Update(model);
				}
				userMessage.PropertyChanged += MessagePropertyChanged;

				_unsubscribers.Add(userMessage, () =>
				{
					userMessage.PropertyChanged -= MessagePropertyChanged;
				});
			}
			else if (message is AssistantMessage assistantMessage)
			{
				async void MessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
				{
					model.ReasoningContent = assistantMessage.ReasoningContent;
					model.Content = assistantMessage.Content;
					model.Error = assistantMessage.Error;
					model.Sender = assistantMessage.SenderAgentId.ToString();
					model.Status = assistantMessage.Status switch
					{
						AssistantMessageStatus.Pending => MessageStatusModel.Pending,
						AssistantMessageStatus.Success => MessageStatusModel.Success,
						AssistantMessageStatus.Error => MessageStatusModel.Error,
						AssistantMessageStatus.Cancelled => MessageStatusModel.Cancelled,
						_ => MessageStatusModel.Pending
					};

					database.Messages.Update(model);
				}
				void ToolCallsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
				{
					if (e.NewItems != null)
						foreach (ToolCall newToolCall in e.NewItems)
							CreateAndInsertToolCallModelAndSubscribe(newToolCall, assistantMessage, model);
				}
				assistantMessage.PropertyChanged += MessagePropertyChanged;
				assistantMessage.ToolCalls.CollectionChanged += ToolCallsCollectionChanged;

				_unsubscribers.Add(assistantMessage, () =>
				{
					assistantMessage.PropertyChanged -= MessagePropertyChanged;
					assistantMessage.ToolCalls.CollectionChanged -= ToolCallsCollectionChanged;
				});
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(message), "Invalid message type");
			}

			void AttachmentsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
			{
				if (e.OldItems != null)
					foreach (Attachment oldAttachment in e.OldItems)
						database.Attachments.DeleteMany(avm => avm.Attachment.Guid == oldAttachment.Guid);

				if (e.NewItems != null)
					foreach (Attachment newAttachment in e.NewItems)
					{
						var attachmentModel = new AttachmentModel
						{
							IsParentToolCall = false,
							ParentId = model.Id,
							Attachment = newAttachment
						};
						database.Attachments.Insert(attachmentModel);
					}
			}
			message.Attachments.CollectionChanged += AttachmentsCollectionChanged;

			void AdditionalViewModelsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
			{
				if (e.OldItems != null)
					foreach (AdditionalMessageViewModel oldVm in e.OldItems)
						database.AdditionalMessageViewModels.DeleteMany(avm => avm.ViewModel.Guid == oldVm.Guid);

				if (e.NewItems != null)
					foreach (AdditionalMessageViewModel newVm in e.NewItems)
						CreateAndInsertAdditionalViewModelAndSubscribe(newVm, message, model);
			}
			message.AdditionalViewModels.CollectionChanged += AdditionalViewModelsCollectionChanged;

			_unsubscribers.Add(message, () =>
			{
				message.Attachments.CollectionChanged -= AttachmentsCollectionChanged;
				message.AdditionalViewModels.CollectionChanged -= AdditionalViewModelsCollectionChanged;
			});
		}

		private void Unsubscribe(ChatMessage message)
		{
			if (_unsubscribers.TryGetValue(message, out var actions))
				foreach (var unsub in actions)
					unsub.Invoke();
			message.Dispose();
			_unsubscribers.Remove(message);
		}

		private MessageModel CreateAndInsertMessageModel(ChatMessage message)
		{
			if (message is UserMessage userMessage)
			{
				var model = new MessageModel
				{
					CreatedAt = userMessage.CreatedAt,
					Content = userMessage.Content,
					Sender = userMessage.SenderLogin,
					Visibility = userMessage.Visibility,
					VisibleTo = userMessage.VisibleTo,
					IsVisibleToWhiteList = userMessage.IsVisibleToWhiteList,
					Role = RoleModel.User
				};

				database.Messages.Insert(model);

				foreach (var attachment in message.Attachments)
				{
					var attachmentModel = new AttachmentModel
					{
						IsParentToolCall = false,
						ParentId = model.Id,
						Attachment = attachment
					};
					database.Attachments.Insert(attachmentModel);
				}

				foreach (var additionalViewModel in message.AdditionalViewModels)
				{
					CreateAndInsertAdditionalViewModelAndSubscribe(additionalViewModel, message, model);
				}

				SubscribeMessage(message, model);

				return model;
			}
			else if (message is AssistantMessage assistantMessage)
			{
				var model = new MessageModel
				{
					CreatedAt = assistantMessage.CreatedAt,
					Sender = assistantMessage.SenderAgentId.ToString(),
					AgentStageId = assistantMessage.AgentStageId,
					ReasoningContent = assistantMessage.ReasoningContent,
					Content = assistantMessage.Content,
					Error = assistantMessage.Error,
					Status = assistantMessage.Status switch
					{
						AssistantMessageStatus.Pending => MessageStatusModel.Pending,
						AssistantMessageStatus.Success => MessageStatusModel.Success,
						AssistantMessageStatus.Error => MessageStatusModel.Error,
						AssistantMessageStatus.Cancelled => MessageStatusModel.Cancelled,
						_ => MessageStatusModel.Pending
					},
					Role = RoleModel.Assistant
				};

				database.Messages.Insert(model);

				foreach (var toolCall in assistantMessage.ToolCalls)
				{
					CreateAndInsertToolCallModelAndSubscribe(toolCall, assistantMessage, model);
				}

				foreach (var attachment in message.Attachments)
				{
					var attachmentModel = new AttachmentModel
					{
						IsParentToolCall = false,
						ParentId = model.Id,
						Attachment = attachment
					};
					database.Attachments.Insert(attachmentModel);
				}

				foreach (var additionalViewModel in message.AdditionalViewModels)
				{
					CreateAndInsertAdditionalViewModelAndSubscribe(additionalViewModel, message, model);
				}

				SubscribeMessage(message, model);

				return model;
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(message), "Invalid message type");
			}
		}

		private ChatMessage CreateChatMessage(MessageModel messageModel)
		{
			if (messageModel.Role == RoleModel.User)
			{
				var attachments = database.Attachments.Find(a => !a.IsParentToolCall && a.ParentId == messageModel.Id)
					.Select(am => am.Attachment);

				var additionalViewModels = database.AdditionalMessageViewModels
					.Find(avm => avm.MessageId == messageModel.Id)
					.OrderBy(avm => avm.Id);

				var result = new UserMessage
				{
					CreatedAt = messageModel.CreatedAt,
					Content = messageModel.Content,
					Attachments = [.. attachments],
					SenderLogin = messageModel.Sender,
					Visibility = messageModel.Visibility,
					VisibleTo = messageModel.VisibleTo,
					IsVisibleToWhiteList = messageModel.IsVisibleToWhiteList
				};

				foreach (var additionalViewDataModel in additionalViewModels)
				{
					SubscribeAdditionalViewModel(additionalViewDataModel.ViewModel, result, messageModel, additionalViewDataModel);
					result.AdditionalViewModels.Add(additionalViewDataModel.ViewModel);
				}

				SubscribeMessage(result, messageModel);

				return result;
			}
			else if (messageModel.Role == RoleModel.Assistant)
			{
				var toolCallModels = database.ToolCalls.Find(t => t.MessageId == messageModel.Id).ToList();

				var attachments = database.Attachments.Find(a => !a.IsParentToolCall && a.ParentId == messageModel.Id)
					.Select(am => am.Attachment);

				var additionalViewModels = database.AdditionalMessageViewModels
					.Find(avm => avm.MessageId == messageModel.Id)
					.OrderBy(avm => avm.Id);

				var result = new AssistantMessage
				{
					CreatedAt = messageModel.CreatedAt,
					SenderAgentId = Guid.TryParse(messageModel.Sender, out var senderAgent) ? senderAgent : Guid.Empty,
					AgentStageId = messageModel.AgentStageId,
					ReasoningContent = messageModel.ReasoningContent,
					Content = messageModel.Content,
					Attachments = [.. attachments],
					CompletionToken = CompletionToken.Success,
					Error = messageModel.Error
				};

				foreach (var additionalViewDataModel in additionalViewModels)
				{
					SubscribeAdditionalViewModel(additionalViewDataModel.ViewModel, result, messageModel, additionalViewDataModel);
					result.AdditionalViewModels.Add(additionalViewDataModel.ViewModel);
				}

				foreach (var toolCallModel in toolCallModels)
				{
					var toolAttachments = database.Attachments.Find(a => a.IsParentToolCall && a.ParentId == toolCallModel.Id)
						.Select(am => am.Attachment);

					var toolCall = new ToolCall
					{
						Id = toolCallModel.ToolCallId,
						ToolName = toolCallModel.ToolName,
						Title = toolCallModel.Title,
						Arguments = toolCallModel.FunctionArguments,
						ResultContent = toolCallModel.ResultContent,
						Attachments = [.. toolAttachments],
						UseMarkdown = toolCallModel.UseMarkdown,
						StructuredResult = string.IsNullOrEmpty(toolCallModel.StructuredResult) ? null
							: JsonNode.Parse(toolCallModel.StructuredResult),
						Status = toolCallModel.Status switch
						{
							ToolStatusModel.NotExecuted => ToolStatus.None,
							ToolStatusModel.ExecutionBegin => ToolStatus.ExecutionInterrupted,
							ToolStatusModel.Success => ToolStatus.Success,
							ToolStatusModel.Cancelled => ToolStatus.Cancelled,
							ToolStatusModel.Error => ToolStatus.Error,
							_ => ToolStatus.None
						},
						StatusIcon = toolCallModel.StatusIcon,
						StatusTitle = toolCallModel.StatusTitle,
						ExpectedBehaviour = toolCallModel.ExpectedBehaviour,
						CompletionToken = CompletionToken.Success
					};

					result.ToolCalls.Add(toolCall);

					SubscribeToolCall(toolCall, result, toolCallModel);
				}

				SubscribeMessage(result, messageModel);

				return result;
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(messageModel.Role), "Invalid role");
			}
		}

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
	}
}