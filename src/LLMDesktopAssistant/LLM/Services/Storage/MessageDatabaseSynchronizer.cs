using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.Data;
using LLMDesktopAssistant.Data.ChatModels;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM.Additional;
using LLMDesktopAssistant.Tools;
using RCLargeLanguageModels.Tasks;

namespace LLMDesktopAssistant.LLM.Services.Storage
{
	/// <summary>
	/// Synchronizes ONE chat message (domain object) with its <see cref="MessageModel"/> and everything
	/// nested inside it (tool calls with their additional view models, message additional view models).
	/// Created separately for each message, owner is <see cref="IChatStorageService"/>.
	/// </summary>
	public class MessageDatabaseSynchronizer : Disposable
	{
		private readonly ChatDatabase _database;

		/// <summary>
		/// The domain object being synchronized. Never null after creation.
		/// </summary>
		public ChatMessage Target { get; }

		/// <summary>
		/// The database model of the message. Always exists: brand new messages are inserted
		/// right in the constructor.
		/// </summary>
		private readonly MessageModel _model;

		/// <summary>
		/// Nested synchronizers for tool calls, keyed by the domain tool call object.
		/// </summary>
		private readonly Dictionary<ToolCall, ToolCallDatabaseSynchronizer> _toolCalls = [];

		/// <summary>
		/// Nested synchronizers for additional view models, keyed by the domain view model.
		/// </summary>
		private readonly Dictionary<AdditionalMessageViewModel, AdditionalViewModelDatabaseSynchronizer> _additionalViewModels = [];

		// =====================================================================================
		// Creation paths
		// =====================================================================================

		/// <summary>
		/// Creates a synchronizer for a BRAND NEW message: maps the domain object to a <see cref="MessageModel"/>,
		/// inserts it into the database, assigns <see cref="ChatObjectBase.Id"/> to <see cref="Target"/>
		/// from the generated ID and subscribes to all changes. Nested entities (tool calls, additional
		/// view models) that are already present in the target collections are inserted as well.
		/// </summary>
		public MessageDatabaseSynchronizer(ChatDatabase database, ChatMessage target)
		{
			_database = database;
			Target = target;
			_model = CreateModelAndInsert(target);
			target.Id = _model.Id;

			AttachExistingChildren();
			Subscribe();
		}

		/// <summary>
		/// THE ONLY place where a message is loaded from the database.
		/// Builds the domain object from <paramref name="model"/> (including all nested tool calls and
		/// additional view models), then creates a synchronizer in "already stored" mode:
		/// no inserts, only subscriptions.
		/// </summary>
		public static MessageDatabaseSynchronizer CreateFromModel(ChatDatabase database, MessageModel model)
		{
			var target = CreateMessageFromModel(model);

			// Load tool calls with their additional view models.
			var toolCallModels = database.ToolCalls.Find(t => t.MessageId == model.Id).ToList();
			foreach (var toolCallModel in toolCallModels)
			{
				var toolCall = CreateToolCallFromModel(toolCallModel);
				foreach (var viewDataModel in database.AdditionalMessageViewModels
					.Find(avm => avm.ParentKind == VMParentKind.ToolCall && avm.ParentId == toolCallModel.Id)
					.OrderBy(avm => avm.Id))
				{
					toolCall.AdditionalViewModels.Add(viewDataModel.ViewModel);
				}
				target.ToolCalls.Add(toolCall);
			}

			// Load message additional view models.
			foreach (var viewDataModel in database.AdditionalMessageViewModels
				.Find(avm => avm.ParentKind == VMParentKind.Message && avm.ParentId == model.Id)
				.OrderBy(avm => avm.Id))
			{
				target.AdditionalViewModels.Add(viewDataModel.ViewModel);
			}

			return new MessageDatabaseSynchronizer(database, target, model);
		}

		/// <summary>
		/// Core constructor for an ALREADY STORED message (loaded from database):
		/// the model exists, children are already present in the target collections,
		/// so only subscriptions and nested synchronizer attachments are made.
		/// </summary>
		private MessageDatabaseSynchronizer(ChatDatabase database, ChatMessage target, MessageModel model)
		{
			_database = database;
			Target = target;
			_model = model;

			AttachExistingChildren();
			Subscribe();
		}

		// =====================================================================================
		// Children management
		// =====================================================================================

		/// <summary>
		/// Creates nested synchronizers for tool calls and additional view models that are already
		/// present in the target collections. The nested synchronizers are created with a null model:
		/// they find their own database rows by the target IDs (loaded objects) or insert new rows
		/// (brand new objects).
		/// </summary>
		private void AttachExistingChildren()
		{
			foreach (var toolCall in Target.ToolCalls)
				_toolCalls[toolCall] = new ToolCallDatabaseSynchronizer(_database, toolCall, _model.Id, model: null);

			foreach (var viewModel in Target.AdditionalViewModels)
				_additionalViewModels[viewModel] = CreateAdditionalViewModelSynchronizer(viewModel);
		}

		private AdditionalViewModelDatabaseSynchronizer CreateAdditionalViewModelSynchronizer(AdditionalMessageViewModel viewModel)
			=> new(_database, viewModel, model: null, VMParentKind.Message, _model.Id);

		// =====================================================================================
		// Database operations
		// =====================================================================================

		/// <summary>
		/// Removes the message and ALL its nested entities (tool calls, their additional view models,
		/// message additional view models) from the database. Disposes this synchronizer.
		/// Called when the node with this message is deleted.
		/// </summary>
		public void Delete()
		{
			DeleteFromDatabase(_database, _model.Id);
			Dispose();
		}

		/// <summary>
		/// Removes the message row and ALL its nested rows (tool calls with their additional view models,
		/// message additional view models) from the database. Used when no domain object / synchronizer
		/// exists for the message (for example, when deleting whole side branches that are not loaded).
		/// </summary>
		public static void DeleteFromDatabase(ChatDatabase database, int messageId)
		{
			foreach (var toolCall in database.ToolCalls.Find(t => t.MessageId == messageId).ToList())
				database.AdditionalMessageViewModels.DeleteMany(avm => avm.ParentKind == VMParentKind.ToolCall && avm.ParentId == toolCall.Id);
			database.ToolCalls.DeleteMany(t => t.MessageId == messageId);

			database.AdditionalMessageViewModels.DeleteMany(avm => avm.ParentKind == VMParentKind.Message && avm.ParentId == messageId);
			database.Messages.Delete(messageId);
		}

		// =====================================================================================
		// Subscriptions (mapping domain changes to the model)
		// =====================================================================================

		private void Subscribe()
		{
			Target.PropertyChanged += OnTargetPropertyChanged;
			Target.ToolCalls.CollectionChanged += OnToolCallsCollectionChanged;
			Target.AdditionalViewModels.CollectionChanged += OnAdditionalViewModelsCollectionChanged;
		}

		private void OnTargetPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			CopyToModel();
			_database.Messages.Update(_model);
		}

		private void OnToolCallsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
				foreach (ToolCall oldToolCall in e.OldItems)
				{
					if (_toolCalls.TryGetValue(oldToolCall, out var sync))
					{
						sync.Delete();
						_toolCalls.Remove(oldToolCall);
					}
				}

			if (e.NewItems != null)
				foreach (ToolCall newToolCall in e.NewItems)
					_toolCalls[newToolCall] = new ToolCallDatabaseSynchronizer(_database, newToolCall, _model.Id, model: null);
		}

		private void OnAdditionalViewModelsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
				foreach (AdditionalMessageViewModel oldVm in e.OldItems)
				{
					if (_additionalViewModels.TryGetValue(oldVm, out var sync))
					{
						sync.Delete();
						_additionalViewModels.Remove(oldVm);
					}
				}

			if (e.NewItems != null)
				foreach (AdditionalMessageViewModel newVm in e.NewItems)
					_additionalViewModels[newVm] = CreateAdditionalViewModelSynchronizer(newVm);
		}

		// =====================================================================================
		// Mapping helpers
		// =====================================================================================

		/// <summary>
		/// Copies the current domain values to the model. Called on every property change.
		/// </summary>
		private void CopyToModel()
		{
			_model.Content = Target.Content;

			switch (Target)
			{
				case UserMessage userMessage:
					_model.Sender = userMessage.SenderLogin;
					_model.Visibility = userMessage.Visibility;
					_model.IsRevealed = userMessage.IsRevealed;
					_model.VisibleTo = userMessage.VisibleTo;
					_model.IsVisibleToWhiteList = userMessage.IsVisibleToWhiteList;
					break;

				case AssistantMessage assistantMessage:
					_model.Sender = assistantMessage.SenderAgentId.ToString();
					_model.AgentStageId = assistantMessage.AgentStageId;
					_model.IsUserLike = assistantMessage.IsUserLike;
					_model.ReasoningContent = assistantMessage.ReasoningContent;
					_model.Error = assistantMessage.Error;
					_model.Status = assistantMessage.Status switch
					{
						AssistantMessageStatus.Pending => MessageStatusModel.Pending,
						AssistantMessageStatus.Success => MessageStatusModel.Success,
						AssistantMessageStatus.Error => MessageStatusModel.Error,
						AssistantMessageStatus.Cancelled => MessageStatusModel.Cancelled,
						_ => MessageStatusModel.Pending
					};
					break;
			}
		}

		/// <summary>
		/// Builds a <see cref="MessageModel"/> from the domain object (by role) and inserts it
		/// into the database. Used for brand new messages.
		/// </summary>
		private MessageModel CreateModelAndInsert(ChatMessage message)
		{
			var model = new MessageModel
			{
				CreatedAt = message.CreatedAt,
				Content = message.Content
			};

			switch (message)
			{
				case UserMessage userMessage:
					model.Role = RoleModel.User;
					model.Sender = userMessage.SenderLogin;
					model.Visibility = userMessage.Visibility;
					model.IsRevealed = userMessage.IsRevealed;
					model.VisibleTo = userMessage.VisibleTo;
					model.IsVisibleToWhiteList = userMessage.IsVisibleToWhiteList;
					break;

				case AssistantMessage assistantMessage:
					model.Role = RoleModel.Assistant;
					model.Sender = assistantMessage.SenderAgentId.ToString();
					model.AgentStageId = assistantMessage.AgentStageId;
					model.IsUserLike = assistantMessage.IsUserLike;
					model.ReasoningContent = assistantMessage.ReasoningContent;
					model.Error = assistantMessage.Error;
					model.Status = assistantMessage.Status switch
					{
						AssistantMessageStatus.Pending => MessageStatusModel.Pending,
						AssistantMessageStatus.Success => MessageStatusModel.Success,
						AssistantMessageStatus.Error => MessageStatusModel.Error,
						AssistantMessageStatus.Cancelled => MessageStatusModel.Cancelled,
						_ => MessageStatusModel.Pending
					};
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(message), "Invalid message type");
			}

			_database.Messages.Insert(model);
			return model;
		}

		private static ChatMessage CreateMessageFromModel(MessageModel model)
		{
			switch (model.Role)
			{
				case RoleModel.User:
					return new UserMessage
					{
						Id = model.Id,
						CreatedAt = model.CreatedAt,
						Content = model.Content,
						SenderLogin = model.Sender,
						Visibility = model.Visibility,
						IsRevealed = model.IsRevealed,
						VisibleTo = model.VisibleTo,
						IsVisibleToWhiteList = model.IsVisibleToWhiteList
					};

				case RoleModel.Assistant:
					return new AssistantMessage
					{
						Id = model.Id,
						CreatedAt = model.CreatedAt,
						SenderAgentId = Guid.TryParse(model.Sender, out var senderAgent) ? senderAgent : Guid.Empty,
						AgentStageId = model.AgentStageId,
						IsUserLike = model.IsUserLike,
						ReasoningContent = model.ReasoningContent,
						Content = model.Content,
						Error = model.Error,
						Status = model.Status switch
						{
							MessageStatusModel.Pending => AssistantMessageStatus.Pending,
							MessageStatusModel.Success => AssistantMessageStatus.Success,
							MessageStatusModel.Error => AssistantMessageStatus.Error,
							MessageStatusModel.Cancelled => AssistantMessageStatus.Cancelled,
							_ => AssistantMessageStatus.Pending
						},
						CompletionToken = CompletionToken.Success
					};

				default:
					throw new ArgumentOutOfRangeException(nameof(model.Role), "Invalid role");
			}
		}

		private static ToolCall CreateToolCallFromModel(ToolCallModel model)
		{
			return new ToolCall
			{
				Id = model.Id,
				ToolCallId = model.ToolCallId,
				ToolName = model.ToolName,
				Title = model.Title,
				Arguments = model.FunctionArguments,
				ResultContent = model.ResultContent,
				UseMarkdown = model.UseMarkdown,
				StructuredResult = string.IsNullOrEmpty(model.StructuredResult) ? null
					: JsonNode.Parse(model.StructuredResult),
				Status = model.Status switch
				{
					ToolStatusModel.NotExecuted => ToolStatus.None,
					ToolStatusModel.ExecutionBegin => ToolStatus.ExecutionInterrupted,
					ToolStatusModel.Success => ToolStatus.Success,
					ToolStatusModel.Cancelled => ToolStatus.Cancelled,
					ToolStatusModel.Error => ToolStatus.Error,
					_ => ToolStatus.None
				},
				StatusIcon = model.StatusIcon,
				StatusTitle = model.StatusTitle,
				ExpectedBehaviour = model.ExpectedBehaviour,
				CompletionToken = CompletionToken.Success
			};
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				Target.PropertyChanged -= OnTargetPropertyChanged;
				Target.ToolCalls.CollectionChanged -= OnToolCallsCollectionChanged;
				Target.AdditionalViewModels.CollectionChanged -= OnAdditionalViewModelsCollectionChanged;

				// Dispose nested synchronizers WITHOUT deleting rows - the message stays in the database.
				foreach (var sync in _toolCalls.Values)
					sync.Dispose();
				foreach (var sync in _additionalViewModels.Values)
					sync.Dispose();
			}
		}
	}
}
