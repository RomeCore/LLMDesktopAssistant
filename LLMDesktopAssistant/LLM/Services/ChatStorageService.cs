using LLMDesktopAssistant.Data;
using LLMDesktopAssistant.Data.ChatModels;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Tasks;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;

namespace LLMDesktopAssistant.LLM.Services
{
	public class ChatStorageService(
			Chat chat,
			ChatDatabase database
		) : Disposable, IChatStorageService
	{
		readonly int conversationId = chat.ChatId;
		Action? _mainUnsubscriber;
		readonly MultiValueDictionary<ChatMessage, Action> _unsubscribers = [];

		public void Reload()
		{
			var messages = new List<BranchedMessage>();

			var conversation = database.Chats.FindById(conversationId);
			if (conversation == null)
			{
				conversation = new ChatModel
				{
					Id = conversationId,
					LeafNodeId = -1,
					RootNodeId = -1,
					SettingsProfile = ChatSettings.DefaultId,
					CreatedAt = DateTime.Now,
					LastModifiedAt = DateTime.Now
				};
				database.Chats.Insert(conversation);
			}

			chat.Settings = SettingsManager.Get<ChatSettings>(conversation.SettingsProfile);
			chat.IsTemporary = conversation.IsTemporary;

			var currentNodeId = conversation.RootNodeId;

			while (currentNodeId != -1)
			{
				var nodeModel = database.MessageNodes.FindById(currentNodeId);
				var messageModel = database.Messages.FindById(nodeModel.MessageId);
				var chatMessage = CreateChatMessage(messageModel);
				messages.Add(CreateBranchedMessage(nodeModel, chatMessage, messages.Count));

				currentNodeId = nodeModel.SelectedNodeId;
			}

			_mainUnsubscriber?.Invoke();
			void ChatPropertyChanged(object? s, PropertyChangedEventArgs e)
			{
				conversation = database.Chats.FindById(conversationId);

				conversation.SettingsProfile = chat.Settings.Id;
				conversation.IsTemporary = chat.IsTemporary;

				database.Chats.Update(conversation);
			}
			chat.PropertyChanged += ChatPropertyChanged;
			_mainUnsubscriber = () =>
			{
				chat.PropertyChanged -= ChatPropertyChanged;
			};

			for (int i = 0; i < chat.Messages.Count; i++)
				Unsubscribe(chat.Messages[i].Message);
			chat.Messages.Reset(messages);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			_mainUnsubscriber?.Invoke();
			for (int i = 0; i < chat.Messages.Count; i++)
				Unsubscribe(chat.Messages[i].Message);
		}

		public void AppendMessage(ChatMessage chatMessage)
		{
			if (!database.Database.BeginTrans())
				throw new InvalidOperationException("Failed to begin transaction.");

			var conversation = database.Chats.FindById(conversationId);
			int messageId = CreateAndInsertMessageModel(chatMessage).Id;
			MessageNodeModel nodeModel;

			if (conversation.RootNodeId == -1)
			{
				// Add root node
				int nodeId = database.MessageNodes.Insert(nodeModel = new MessageNodeModel
				{
					IsRootNode = true,
					ParentId = conversationId,
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

		public void SwitchBranch(int messageIndex, int newBranchIndex)
		{
			if (messageIndex < 0 || messageIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(messageIndex));

			if (!database.Database.BeginTrans())
				throw new InvalidOperationException("Failed to begin transaction.");

			var conversation = database.Chats.FindById(conversationId);
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

		public void EditMessage(int editIndex, ChatMessage newMessage)
		{
			if (editIndex < 0 || editIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(editIndex));

			if (!database.Database.BeginTrans())
				throw new InvalidOperationException("Failed to begin transaction.");

			var conversation = database.Chats.FindById(conversationId);
			int currentNodeId = conversation.RootNodeId;
			int currentIndex = 0;

			while (currentIndex < editIndex)
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

			for (int i = editIndex; i < chat.Messages.Count; i++)
				Unsubscribe(chat.Messages[i].Message);
			chat.Messages.ReplaceRange(editIndex, chat.Messages.Count - editIndex, [CreateBranchedMessage(newNode, newMessage, editIndex)]);
		}

		public void PlaceNewBranch(int messageIndex)
		{
			if (messageIndex < 0 || messageIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(messageIndex));

			if (!database.Database.BeginTrans())
				throw new InvalidOperationException("Failed to begin transaction.");

			var conversation = database.Chats.FindById(conversationId);
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



		private ToolCallModel CreateAndInsertToolCallModel(ToolCall toolCall, MessageModel model)
		{
			var toolCallModel = new ToolCallModel
			{
				MessageId = model.Id,
				ToolCallId = toolCall.Id,
				ToolName = toolCall.ToolName,
				Title = toolCall.Title,
				FunctionArguments = JsonSerializer.Serialize(toolCall.Arguments),
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
				ResultContent = toolCall.ResultContent
			};

			database.ToolCalls.Insert(toolCallModel);

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
				model.StatusIcon = toolCall.StatusIcon;
				model.StatusTitle = toolCall.StatusTitle;
				model.ResultContent = toolCall.ResultContent;

				database.ToolCalls.Update(model);
			}
			toolCall.PropertyChanged += OnToolCallPropertyChanged;

			_unsubscribers.Add(assistantMessage, () =>
			{
				toolCall.PropertyChanged -= OnToolCallPropertyChanged;
			});
		}

		private void CreateAndInsertToolCallModelAndSubscribe(ToolCall toolCall, AssistantMessage assistantMessage, MessageModel model)
		{
			var toolCallModel = CreateAndInsertToolCallModel(toolCall, model);

			SubscribeToolCall(toolCall, assistantMessage, toolCallModel);
		}

		private AdditionalMessageViewDataModel CreateAndInsertAdditionalViewModel(AdditionalMessageViewModel viewModel, MessageModel model)
		{
			var additionalViewDataModel = new AdditionalMessageViewDataModel
			{
				MessageId = model.Id,
				ViewModel = viewModel
			};

			database.AdditionalMessageViewModels.Insert(additionalViewDataModel);

			return additionalViewDataModel;
		}

		private void SubscribeAdditionalViewModel(AdditionalMessageViewModel viewModel, ChatMessage message, AdditionalMessageViewDataModel model)
		{
			async void OnAdditionalViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
			{
				database.AdditionalMessageViewModels.Update(model);
			}
			viewModel.PropertyChanged += OnAdditionalViewModelPropertyChanged;

			_unsubscribers.Add(message, () =>
			{
				viewModel.PropertyChanged -= OnAdditionalViewModelPropertyChanged;
			});
		}

		private void CreateAndInsertAdditionalViewModelAndSubscribe(AdditionalMessageViewModel viewModel, ChatMessage message, MessageModel model)
		{
			var additionalViewDataModel = CreateAndInsertAdditionalViewModel(viewModel, model);

			SubscribeAdditionalViewModel(viewModel, message, additionalViewDataModel);
		}

		private void SubscribeMessage(ChatMessage message, MessageModel model)
		{
			if (message is UserMessage userMessage)
			{
				async void MessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
				{
					model.CreatedAt = userMessage.CreatedAt;
					model.SummaryOfPrevMessages = userMessage.SummaryOfPrevMessages;
					model.Content = userMessage.Content;
					model.LLMProvidedContent = userMessage.LLMProvidedContent;

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
					model.CreatedAt = assistantMessage.CreatedAt;
					model.SummaryOfPrevMessages = assistantMessage.SummaryOfPrevMessages;
					model.ReasoningContent = assistantMessage.ReasoningContent;
					model.Content = assistantMessage.Content;
					model.Error = assistantMessage.Error;
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

			void AdditionalViewModelsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
			{
				if (e.OldItems != null)
					foreach (AdditionalMessageViewModel oldVm in e.OldItems)
						database.AdditionalMessageViewModels.DeleteMany(avm => avm.ViewModel.Guid == oldVm.Guid);

				if (e.NewItems != null)
					foreach (AdditionalMessageViewModel newVm in e.NewItems)
						if (!newVm.IsTemporary)
							CreateAndInsertAdditionalViewModelAndSubscribe(newVm, message, model);
			}
			message.AdditionalViewModels.CollectionChanged += AdditionalViewModelsCollectionChanged;

			_unsubscribers.Add(message, () =>
			{
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
					SummaryOfPrevMessages = message.SummaryOfPrevMessages,
					Content = userMessage.Content,
					LLMProvidedContent = userMessage.LLMProvidedContent,
					Role = RoleModel.User
				};

				database.Messages.Insert(model);
				int messageId = model.Id;

				foreach (var attachment in userMessage.Attachments)
				{
					var attachmentModel = new AttachmentModel
					{
						MessageId = messageId,
						Title = attachment.Title,
						SourceUrl = attachment.SourceUrl,
						LocalPath = attachment.LocalPath,
						Size = attachment.Size,
						AdditionalInfo = attachment.AdditionalInfo,
						PreviewContent = attachment.PreviewContent
					};
					database.Attachments.Insert(attachmentModel);
				}

				foreach (var additionalViewModel in message.AdditionalViewModels)
				{
					if (!additionalViewModel.IsTemporary)
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
					SummaryOfPrevMessages = assistantMessage.SummaryOfPrevMessages,
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

				foreach (var additionalViewModel in message.AdditionalViewModels)
				{
					if (!additionalViewModel.IsTemporary)
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
				var attachments = database.Attachments.Find(a => a.MessageId == messageModel.Id)
					.Select(am => new Attachment
					{
						Title = am.Title,
						SourceUrl = am.SourceUrl,
						LocalPath = am.LocalPath,
						Size = am.Size,
						AdditionalInfo = am.AdditionalInfo,
						PreviewContent = am.PreviewContent
					});

				var additionalViewModels = database.AdditionalMessageViewModels
					.Find(avm => avm.MessageId == messageModel.Id)
					.OrderBy(avm => avm.Id);

				var result = new UserMessage
				{
					Content = messageModel.Content,
					LLMProvidedContent = messageModel.LLMProvidedContent,
					SummaryOfPrevMessages = messageModel.SummaryOfPrevMessages,
					Attachments = attachments.ToImmutableList()
				};

				foreach (var additionalViewDataModel in additionalViewModels)
				{
					SubscribeAdditionalViewModel(additionalViewDataModel.ViewModel, result, additionalViewDataModel);
					result.AdditionalViewModels.Add(additionalViewDataModel.ViewModel);
				}

				SubscribeMessage(result, messageModel);

				return result;
			}
			else if (messageModel.Role == RoleModel.Assistant)
			{
				var toolCallModels = database.ToolCalls.Find(t => t.MessageId == messageModel.Id).ToList();

				var additionalViewModels = database.AdditionalMessageViewModels
					.Find(avm => avm.MessageId == messageModel.Id)
					.OrderBy(avm => avm.Id);

				var result = new AssistantMessage
				{
					ReasoningContent = messageModel.ReasoningContent,
					Content = messageModel.Content,
					SummaryOfPrevMessages = messageModel.SummaryOfPrevMessages,
					CompletionToken = CompletionToken.Success,
					Error = messageModel.Error
				};

				foreach (var additionalViewDataModel in additionalViewModels)
				{
					SubscribeAdditionalViewModel(additionalViewDataModel.ViewModel, result, additionalViewDataModel);
					result.AdditionalViewModels.Add(additionalViewDataModel.ViewModel);
				}

				foreach (var toolCallModel in toolCallModels)
				{
					var toolCall = new ToolCall
					{
						Id = toolCallModel.ToolCallId,
						ToolName = toolCallModel.ToolName,
						Title = toolCallModel.Title,
						Arguments = JsonNode.Parse(toolCallModel.FunctionArguments)!,
						ResultContent = toolCallModel.ResultContent,
						Status = toolCallModel.Status switch
						{
							ToolStatusModel.NotExecuted => ToolStatus.None,
							ToolStatusModel.ExecutionBegin => ToolStatus.ExecutionInterrupted,
							ToolStatusModel.Success => ToolStatus.Success,
							ToolStatusModel.Cancelled => ToolStatus.Cancelled,
							ToolStatusModel.Error => ToolStatus.Error,
							ToolStatusModel.NoResult => ToolStatus.NoResult,
							_ => ToolStatus.None
						},
						StatusIcon = toolCallModel.StatusIcon,
						StatusTitle = toolCallModel.StatusTitle,
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
				MessageIndex = messageIndex,
				AvailableBranchesCount = sameOrderNodes.Count,
				SelectedBranchIndex = selectedNode
			};
		}
	}
}