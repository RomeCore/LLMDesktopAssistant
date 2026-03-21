using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using LiteDB;
using LLMDesktopAssistant.LLM.Conversations.Models;
using LLMDesktopAssistant.Utils;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Agents;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.LLM.Conversations
{
	/// <summary>
	/// Manages conversation and its associated data using a conversation database.
	/// </summary>
	public class ConversationManager : INotifyCollectionChanged
	{
		private readonly ConversationDatabase _database;
		private readonly int _conversationId;
		private readonly RangeObservableCollection<ConversationMessage> _messages;

		/// <summary>
		/// Gets the collection of messages for the current conversation.
		/// </summary>
		public ReadOnlyObservableCollection<ConversationMessage> Messages { get; }

		/// <summary>
		/// The event that notifies about changes in the collection of <see cref="ConversationMessage"/>s.
		/// </summary>
		public event NotifyCollectionChangedEventHandler? CollectionChanged;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConversationManager"/> class.
		/// </summary>
		/// <param name="database">The database.</param>
		/// <param name="conversationId">The ID of the conversation to manage.</param>
		public ConversationManager(ConversationDatabase database, int conversationId)
		{
			_database = database;
			_conversationId = conversationId;

			if (_database.Conversations.FindById(_conversationId) == null)
				throw new ArgumentException("Cannot find the conversation with ID: " + _conversationId, nameof(conversationId));

			_messages = [];
			Messages = new ReadOnlyObservableCollection<ConversationMessage>(_messages);
			_messages.CollectionChanged += (s, e) => CollectionChanged?.Invoke(this, e);

			UpdateMessages();
		}
		
		private (IToolCall, IToolMessage?) CreateToolCallPair(ToolCallModel toolCallModel)
		{
			IToolCall toolCall;
			IToolMessage? toolMessage = null;

			if (toolCallModel.ToolType == ToolType.Function)
			{
				var arguments = System.Text.Json.JsonSerializer.Deserialize<JsonNode>(toolCallModel.FunctionArguments);
				toolCall = new FunctionToolCall(toolCallModel.ToolCallId, toolCallModel.ToolName, arguments!);
			}
			else
			{
				throw new InvalidDataException($"Invalid tool type. Tool Type: {toolCallModel.ToolType}.");
			}

			ToolResultStatus? status = toolCallModel.ResultStatus switch
			{
				ToolStatus.Success => ToolResultStatus.Success,
				ToolStatus.Cancelled => ToolResultStatus.Cancelled,
				ToolStatus.Error => ToolResultStatus.Error,
				ToolStatus.NoResult => ToolResultStatus.NoResult,
				_ => null
			};
			var content = toolCallModel.ResultContent ?? "Tool gained no result.";

			if (status != null)
				toolMessage = new ToolMessage(new ToolResult(status.Value, content),
					toolCallModel.ToolCallId, toolCallModel.ToolName);

			return (toolCall, toolMessage);
		}

		private List<IMessage> CreateMessages(MessageModel messageModel)
		{
			var messages = new List<IMessage>();
			if (messageModel.Role == Models.Role.User)
			{
				messages.Add(new UserMessage(messageModel.Content));
			}
			else if (messageModel.Role == Models.Role.Assistant)
			{
				List<IToolCall> toolCalls = [];
				List<IToolMessage> toolMessages = [];

				foreach (var tcModel in _database.ToolCalls.Find(t => t.MessageId == messageModel.Id))
				{
					var (call, message) = CreateToolCallPair(tcModel);
					toolCalls.Add(call);
					if (message != null)
						toolMessages.Add(message);
				}

				messages.Add(new AssistantMessage(messageModel.Content, messageModel.HiddenContent, toolCalls));
				foreach (var message in toolMessages)
					messages.Add(message);
			}
			else
			{
				throw new ArgumentException($"Invalid role. Role: {messageModel.Role}.", nameof(messageModel.Role));
			}

			return messages;
		}

		private int GetCountOfMessages(MessageModel messageModel)
		{
			if (messageModel.Role == Models.Role.User)
			{
				return 1;
			}
			else if (messageModel.Role == Models.Role.Assistant)
			{
				return 1 + _database.ToolCalls.Count(t => t.MessageId == messageModel.Id && t.ResultStatus != null);
			}
			else
			{
				throw new ArgumentException($"Invalid role. Role: {messageModel.Role}.", nameof(messageModel.Role));
			}
		}

		private ExtendedMessage CreateExMessage(MessageModel messageModel)
		{
			if (messageModel.Role == Models.Role.User)
			{
				return new ExtendedMessage
				{
					Message = new UserMessage(messageModel.Content)
				};
			}
			else
			{
				List<IToolCall> toolCalls = [];
				List<IToolMessage> toolMessages = [];

				foreach (var tcModel in _database.ToolCalls.Find(t => t.MessageId == messageModel.Id))
				{
					var (call, message) = CreateToolCallPair(tcModel);
					toolCalls.Add(call);
					if (message != null)
						toolMessages.Add(message);
				}

				return new ExtendedMessage
				{
					Message = new AssistantMessage(messageModel.Content, messageModel.HiddenContent, toolCalls),
					ToolMessages = new(toolMessages.Select(t => new ExtendedMessage { Message = t }))
				};
			}
		}

		private ConversationMessage CreateConvMessage(MessageNodeModel nodeModel, ExtendedMessage exMessage, int order)
		{
			var sameOrderNodes = _database.MessageNodes.Find(n => n.ParentId == nodeModel.ParentId &&
				n.IsRootNode == nodeModel.IsRootNode).Select(n => n.Id).OrderBy(i => i).ToList();
			int selectedNode = sameOrderNodes.IndexOf(nodeModel.Id);

			return new ConversationMessage
			{
				Manager = this,
				Message = exMessage,
				Order = order,
				AvailableBranches = sameOrderNodes.Count,
				BranchIndex = selectedNode
			};
		}

		private int AppendMessage(ExtendedMessage exMessage, MessageModel model)
		{
			if (!_database.Database.BeginTrans())
				throw new InvalidOperationException("Failed to begin transaction.");

			var conversation = _database.Conversations.FindById(_conversationId);
			var messageId = _database.Messages.Insert(model);

			MessageNodeModel nodeModel;

			if (conversation.RootNodeId == -1)
			{
				// Add root node
				int nodeId = _database.MessageNodes.Insert(nodeModel = new MessageNodeModel
				{
					IsRootNode = true,
					ParentId = _conversationId,
					MessageId = messageId
				});
				conversation.RootNodeId = nodeId;
				conversation.LeafNodeId = nodeId;
				_database.Conversations.Update(conversation);
			}
			else
			{
				var leafNode = _database.MessageNodes.FindById(conversation.LeafNodeId);
				int nodeId = _database.MessageNodes.Insert(nodeModel = new MessageNodeModel
				{
					IsRootNode = false,
					ParentId = leafNode.Id,
					MessageId = messageId
				});

				leafNode.SelectedNodeId = nodeId;
				conversation.LeafNodeId = nodeId;
				_database.MessageNodes.Update(leafNode);
				_database.Conversations.Update(conversation);
			}

			if (!_database.Database.Commit())
				throw new InvalidOperationException("Failed to commit transaction.");

			_messages.Add(CreateConvMessage(nodeModel, exMessage, _messages.Count));

			return messageId;
		}

		private void AddToolCall(IToolCall toolCall, int messageId)
		{
			if (toolCall is not FunctionToolCall functionToolCall)
				throw new ArgumentException("Invalid tool call type.", nameof(toolCall));

			var toolCallModel = new ToolCallModel
			{
				MessageId = messageId,
				ToolCallId = toolCall.Id,
				ToolName = toolCall.ToolName,
				ToolType = ToolType.Function,
				FunctionArguments = functionToolCall.Args.ToJsonString()
			};
			_database.ToolCalls.Insert(toolCallModel);
		}

		private void UpdateAssistantMessage(IAssistantMessage message, int messageId)
		{
			var model = _database.Messages.FindById(messageId);
			model.Content = message.Content ?? string.Empty;
			model.HiddenContent = message.ReasoningContent ?? string.Empty;
			_database.Messages.Update(model);
		}

		private void AddMessage(ExtendedMessage extendedMessage, Func<ExtendedMessage, MessageModel, int> addFunc)
		{
			var message = extendedMessage.Message;
			if (message is ISystemMessage)
				throw new InvalidOperationException("Cannot add a system message to the conversation. Use SystemInstructions property instead.");

			if (message is IUserMessage userMessage)
			{
				var model = new MessageModel
				{
					Content = userMessage.Content ?? string.Empty,
					Role = Models.Role.User,
					CreatedAt = DateTime.Now
				};
				addFunc(extendedMessage, model);
			}
			else if (message is IAssistantMessage assistantMessage)
			{
				var model = new MessageModel
				{
					Content = assistantMessage.Content ?? string.Empty,
					HiddenContent = assistantMessage.ReasoningContent,
					Role = Models.Role.Assistant,
					CreatedAt = DateTime.Now
				};
				var messageId = addFunc(extendedMessage, model);

				foreach (var toolCall in assistantMessage.ToolCalls)
					AddToolCall(toolCall, messageId);

				if (message is PartialAssistantMessage partialAssistantMessage &&
					!partialAssistantMessage.CompletionToken.IsCompleted)
				{
					void PartAdded(object? s, AssistantMessageDelta e)
					{
						if (e.NewToolCalls != null && e.NewToolCalls.Count > 0)
						{
							foreach (var toolCall in e.NewToolCalls)
								AddToolCall(toolCall, messageId);
						}
					}
					void Completed(object? s, CompletedEventArgs e)
					{
						partialAssistantMessage.PartAdded -= PartAdded;
						partialAssistantMessage.Completed -= Completed;

						UpdateAssistantMessage(partialAssistantMessage, messageId);
					}

					partialAssistantMessage.PartAdded += PartAdded;
					partialAssistantMessage.Completed += Completed;
				}
			}
			else if (message is IToolMessage toolMessage)
			{
				var toolCallModel = _database.ToolCalls.Find(tc => tc.ToolCallId == toolMessage.ToolCallId)
					.First();

				toolCallModel.ResultContent = toolMessage.Content ?? "Tool gained no result.";
				toolCallModel.ResultStatus = toolMessage.Status switch
				{
					ToolResultStatus.Success => ToolStatus.Success,
					ToolResultStatus.Cancelled => ToolStatus.Cancelled,
					ToolResultStatus.Error => ToolStatus.Error,
					ToolResultStatus.NoResult => ToolStatus.NoResult,
					_ => ToolStatus.NoResult
				};
				_database.ToolCalls.Update(toolCallModel);

				var lastAssistantMessage = _messages[^1].Message;
				if (lastAssistantMessage.Message is not IAssistantMessage)
					throw new InvalidOperationException("Cannot add a tool message to the conversation without an assistant message.");
				
				if (lastAssistantMessage.ToolMessages.FirstOrDefault(t => ((IToolMessage)t.Message).ToolCallId == toolCallModel.ToolCallId)
					is ExtendedMessage messageToReplace)
				{
					lastAssistantMessage.ToolMessages[lastAssistantMessage.ToolMessages.IndexOf(messageToReplace)] = extendedMessage;
				}
				else
				{
					lastAssistantMessage.ToolMessages.Add(extendedMessage);
				}
			}
		}

		/// <summary>
		/// Gets the message IDs for a specific conversation.
		/// </summary>
		/// <param name="conversationModel">The conversation model.</param>
		/// <param name="skip">The number of messages to skip.</param>
		/// <param name="take">The number of messages to take.</param>
		/// <returns>The message IDs for the specified conversation.</returns>
		/// <exception cref="ArgumentException">The conversation with the specified ID does not exist.</exception>
		public IEnumerable<int> GetMessageIds(int skip = 0, int take = int.MaxValue)
		{
			var conversation = _database.Conversations.FindById(_conversationId);
			var currentNodeId = conversation.RootNodeId;
			int counter = 0;
			while (currentNodeId != -1)
			{
				var currentNode = _database.MessageNodes.FindById(currentNodeId);
				if (currentNode == null)
					throw new InvalidDataException($"Message node not found. ID: {currentNodeId}.");

				var messageId = currentNode.MessageId;
				if (counter > skip + take)
					yield break;
				if (counter >= skip)
					yield return messageId;
				counter++;

				currentNodeId = currentNode.SelectedNodeId;
			}
		}

		/// <summary>
		/// Updates the collection of messages for the current conversation.
		/// </summary>
		public void UpdateMessages()
		{
			var messages = new List<ConversationMessage>();

			var conversation = _database.Conversations.FindById(_conversationId);
			var currentNodeId = conversation.RootNodeId;

			while (currentNodeId != -1)
			{
				var nodeModel = _database.MessageNodes.FindById(currentNodeId);
				var messageModel = _database.Messages.FindById(nodeModel.MessageId);
				var exMessage = CreateExMessage(messageModel);
				messages.Add(CreateConvMessage(nodeModel, exMessage, messages.Count));

				currentNodeId = nodeModel.SelectedNodeId;
			}

			_messages.ReplaceRange(messages);
		}

		/// <summary>
		/// Appends a message to the end of currently selected branch.
		/// </summary>
		/// <param name="message">The message to append.</param>
		public void AppendMessage(IMessage message)
		{
			var extended = new ExtendedMessage
			{
				Message = message,
				ToolMessages = []
			};
			AddMessage(extended, AppendMessage);
		}

		/// <summary>
		/// Appends a message to the end of currently selected branch.
		/// </summary>
		/// <param name="extendedMessage">The message to append.</param>
		public void AppendMessage(ExtendedMessage extendedMessage)
		{
			AddMessage(extendedMessage, AppendMessage);
		}



		/// <summary>
		/// Gets the memory for a conversation.
		/// </summary>
		/// <param name="existingMemory">The existing memory to mutate, if any.</param>
		/// <returns>The memory for the specified conversation. If not found, creates a new one.</returns>
		public SummarizingChatMemory CreateMemory(SummarizingChatMemory? existingMemory = null)
		{
			var conversation = _database.Conversations.FindById(_conversationId);
			var currentNodeId = conversation.LeafNodeId;
			var messages = new List<IMessage>();

			existingMemory ??= new SummarizingChatMemory();
			existingMemory.SystemInstructions = conversation.SystemInstructions;
			existingMemory.Messages = messages;

			while (currentNodeId != -1)
			{
				var nodeModel = _database.MessageNodes.FindById(currentNodeId);
				var messageModel = _database.Messages.FindById(nodeModel.MessageId);

				if (messageModel.SummaryOfPrevMessages != null)
				{
					existingMemory.LatestSummary = messageModel.SummaryOfPrevMessages;
					break;
				}
				else
				{
					var messagesForMessageId = CreateMessages(messageModel);
					messages.InsertRange(0, messagesForMessageId);
				}

				if (nodeModel.IsRootNode)
					currentNodeId = -1;
				else
					currentNodeId = nodeModel.ParentId;
			}

			return existingMemory;
		}

		/// <summary>
		/// Imports a summary from the provided conversation memory.
		/// </summary>
		/// <param name="memory">The conversation memory to import from.</param>
		public void ImportSummaryFromMemory(SummarizingChatMemory memory)
		{
			// Just use counts of messages for now. Later we can add more sophisticated logic.

			var newSummary = memory.LatestSummary;
			var messageCount = memory.Messages.Count;

			if (string.IsNullOrWhiteSpace(newSummary))
				return;

			var conversation = _database.Conversations.FindById(_conversationId);
			var currentNodeId = conversation.LeafNodeId;
			int counter = 0;

			while (currentNodeId != -1)
			{
				var nodeModel = _database.MessageNodes.FindById(currentNodeId);
				var messageModel = _database.Messages.FindById(nodeModel.MessageId);

				if (!string.IsNullOrWhiteSpace(messageModel.SummaryOfPrevMessages))
					break;

				if (counter >= messageCount)
				{
					messageModel.SummaryOfPrevMessages = newSummary;
					_database.Messages.Update(messageModel);
					break;
				}

				counter += GetCountOfMessages(messageModel);

				if (nodeModel.IsRootNode)
					break;
				else
					currentNodeId = nodeModel.ParentId;
			}
		}
	}
}
