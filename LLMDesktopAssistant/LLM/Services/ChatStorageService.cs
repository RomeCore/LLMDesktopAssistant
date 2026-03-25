using LLMDesktopAssistant.LLM.Data;
using LLMDesktopAssistant.LLM.Data.Models;
using LLMDesktopAssistant.LLM.Domain;
using RCLargeLanguageModels.Tasks;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;

namespace LLMDesktopAssistant.LLM.Services
{
	public class ChatStorageService(
			Chat chat,
			ConversationDatabase database
		) : IChatStorageService
	{
		readonly int conversationId = chat.Id;

		public void Reload()
		{
			var messages = new List<BranchedMessage>();

			var conversation = database.Conversations.FindById(conversationId);
			if (conversation == null)
			{
				conversation = new ConversationModel
				{
					Id = conversationId,
					LeafNodeId = -1,
					RootNodeId = -1,
					SystemInstructions = "You are a helpful assistant."
				};
				database.Conversations.Insert(conversation);
			}

			chat.SystemPrompt = conversation.SystemInstructions;
			var currentNodeId = conversation.RootNodeId;

			while (currentNodeId != -1)
			{
				var nodeModel = database.MessageNodes.FindById(currentNodeId);
				var messageModel = database.Messages.FindById(nodeModel.MessageId);
				var chatMessage = CreateChatMessage(messageModel);
				messages.Add(CreateBranchedMessage(nodeModel, chatMessage, messages.Count));

				currentNodeId = nodeModel.SelectedNodeId;
			}

			chat.Messages.ReplaceRange(messages);
		}

		public void AppendMessage(ChatMessage chatMessage)
		{
			if (!database.Database.BeginTrans())
				throw new InvalidOperationException("Failed to begin transaction.");

			var conversation = database.Conversations.FindById(conversationId);
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
				database.Conversations.Update(conversation);
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
				database.MessageNodes.Update(leafNode);
				database.Conversations.Update(conversation);
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

			var conversation = database.Conversations.FindById(conversationId);
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
			database.Conversations.Update(conversation);

			if (!database.Database.Commit())
				throw new InvalidOperationException("Failed to commit transaction.");

			chat.Messages.ReplaceRange(messageIndex, chat.Messages.Count - messageIndex, subsequentMessages);
		}

		public void EditMessage(int editIndex, ChatMessage newMessage)
		{
			if (editIndex < 0 || editIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(editIndex));

			if (!database.Database.BeginTrans())
				throw new InvalidOperationException("Failed to begin transaction.");

			var conversation = database.Conversations.FindById(conversationId);
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
			database.Conversations.Update(conversation);

			if (!database.Database.Commit())
				throw new InvalidOperationException("Failed to commit transaction.");

			chat.Messages.ReplaceRange(editIndex, chat.Messages.Count - editIndex, [CreateBranchedMessage(newNode, newMessage, editIndex)]);
		}

		public void PlaceNewBranch(int messageIndex)
		{
			if (messageIndex < 0 || messageIndex >= chat.Messages.Count)
				throw new ArgumentOutOfRangeException(nameof(messageIndex));

			if (!database.Database.BeginTrans())
				throw new InvalidOperationException("Failed to begin transaction.");

			var conversation = database.Conversations.FindById(conversationId);
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
			database.Conversations.Update(conversation);

			if (!database.Database.Commit())
				throw new InvalidOperationException("Failed to commit transaction.");

			chat.Messages.RemoveRange(messageIndex, chat.Messages.Count - messageIndex);
		}



		private MessageModel CreateAndInsertMessageModel(ChatMessage message)
		{
			if (message is UserMessage userMessage)
			{
				var model = new MessageModel
				{
					SummaryOfPrevMessages = message.SummaryOfPrevMessages,
					Content = userMessage.Content,
					CreatedAt = DateTime.Now,
					Role = RoleModel.User
				};

				database.Messages.Insert(model);

				void MessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
				{
					model.SummaryOfPrevMessages = userMessage.SummaryOfPrevMessages;
					model.Content = userMessage.Content;

					database.Messages.Update(model);
				}
				userMessage.PropertyChanged += MessagePropertyChanged;

				return model;
			}
			else if (message is AssistantMessage assistantMessage)
			{
				var model = new MessageModel
				{
					SummaryOfPrevMessages = assistantMessage.SummaryOfPrevMessages,
					ReasoningContent = assistantMessage.ReasoningContent,
					Content = assistantMessage.Content,
					CreatedAt = DateTime.Now,
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

				void AddToolCall(ToolCall toolCall)
				{
					var toolCallModel = new ToolCallModel
					{
						MessageId = model.Id,
						ToolCallId = toolCall.Id,
						ToolName = toolCall.ToolName,
						FunctionArguments = JsonSerializer.Serialize(toolCall.Arguments),
						ResultContent = toolCall.ResultContent,
						Status = toolCall.Status switch
						{
							ToolStatus.NotExecuted => ToolStatusModel.NotExecuted,
							ToolStatus.Success => ToolStatusModel.Success,
							ToolStatus.Error => ToolStatusModel.Error,
							ToolStatus.Cancelled => ToolStatusModel.Cancelled,
							_ => ToolStatusModel.NotExecuted,
						}
					};

					database.ToolCalls.Insert(toolCallModel);

					void OnToolCallPropertyChanged(object? sender, PropertyChangedEventArgs e)
					{
						toolCallModel.ResultContent = toolCall.ResultContent;
						toolCallModel.Status = toolCall.Status switch
						{
							ToolStatus.NotExecuted => ToolStatusModel.NotExecuted,
							ToolStatus.Success => ToolStatusModel.Success,
							ToolStatus.Error => ToolStatusModel.Error,
							ToolStatus.Cancelled => ToolStatusModel.Cancelled,
							_ => ToolStatusModel.NotExecuted,
						};

						database.ToolCalls.Update(toolCallModel);
					}
					toolCall.PropertyChanged += OnToolCallPropertyChanged;
				}

				foreach (var toolCall in assistantMessage.ToolCalls)
				{
					AddToolCall(toolCall);
				}

				void MessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
				{
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
				void ToolCallCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
				{
					if (e.NewItems != null)
						foreach (ToolCall newToolCall in e.NewItems)
							AddToolCall(newToolCall);
				}
				assistantMessage.PropertyChanged += MessagePropertyChanged;
				assistantMessage.ToolCalls.CollectionChanged += ToolCallCollectionChanged;

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
				var result = new UserMessage
				{
					Content = messageModel.Content
				};

				return result;
			}
			else if (messageModel.Role == RoleModel.Assistant)
			{
				var result = new AssistantMessage
				{
					ReasoningContent = messageModel.ReasoningContent,
					Content = messageModel.Content,
					CompletionToken = CompletionToken.Success
				};

				foreach (var tcModel in database.ToolCalls.Find(t => t.MessageId == messageModel.Id))
				{
					result.ToolCalls.Add(new ToolCall
					{
						Id = tcModel.ToolCallId,
						ToolName = tcModel.ToolName,
						Arguments = JsonNode.Parse(tcModel.FunctionArguments)!,
						ResultContent = tcModel.ResultContent,
						Status = tcModel.Status switch
						{
							ToolStatusModel.NotExecuted => ToolStatus.NotExecuted,
							ToolStatusModel.Success => ToolStatus.Success,
							ToolStatusModel.Cancelled => ToolStatus.Cancelled,
							ToolStatusModel.Error => ToolStatus.Error,
							ToolStatusModel.NoResult => ToolStatus.NoResult,
							_ => ToolStatus.NotExecuted
						},
						CompletionToken = CompletionToken.Success
					});
				}

				return result;
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(messageModel.Role), "Invalid role");
			}
		}

		private BranchedMessage CreateBranchedMessage(MessageNodeModel nodeModel, ChatMessage exMessage, int messageIndex)
		{
			var sameOrderNodes = database.MessageNodes.Find(n => n.ParentId == nodeModel.ParentId &&
				n.IsRootNode == nodeModel.IsRootNode).Select(n => n.Id).OrderBy(i => i).ToList();
			int selectedNode = sameOrderNodes.IndexOf(nodeModel.Id);

			return new BranchedMessage
			{
				Message = exMessage,
				MessageIndex = messageIndex,
				AvailableBranchesCount = sameOrderNodes.Count,
				SelectedBranchIndex = selectedNode
			};
		}
	}
}