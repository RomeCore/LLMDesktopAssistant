using RCLargeLanguageModels.Tasks;
using System.Collections.ObjectModel;

namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents a message from the LLM assistant.
	/// </summary>
	public class AssistantMessage : ChatMessage
	{
		/// <summary>
		/// The agent ID that sent the message.
		/// </summary>
		public required Guid SenderAgentId { get; init; }

		/// <summary>
		/// The stage ID that the agent is currently in.
		/// </summary>
		public required Guid AgentStageId { get; init; }

		private string? _reasoningContent = null;
		/// <summary>
		/// Gets or sets the reasoning content of the message.
		/// </summary>
		public string? ReasoningContent
		{
			get => _reasoningContent;
			set => SetProperty(ref _reasoningContent, value);
		}

		/// <summary>
		/// The collection of tool calls associated with this message. If no tool calls were made, this property is an empty collection.
		/// </summary>
		public ObservableCollection<ToolCall> ToolCalls { get; } = [];

		private AssistantMessageStatus _status = AssistantMessageStatus.Pending;
		/// <summary>
		/// Gets or sets the status of the message.
		/// </summary>
		public AssistantMessageStatus Status
		{
			get => _status;
			set => SetProperty(ref _status, value);
		}

		private string? _pendingToolName;
		/// <summary>
		/// Gets or sets the name of the tool that is currently pending. If no tool call is pending, this property is null.
		/// </summary>
		public string? PendingToolName
		{
			get => _pendingToolName;
			set => SetProperty(ref _pendingToolName, value);
		}

		private string? _error;
		/// <summary>
		/// Gets or sets the error message associated with the message. If no error occurred, this property is <see langword="null">.
		/// </summary>
		public string? Error
		{
			get => _error;
			set => SetProperty(ref _error, value);
		}

		/// <summary>
		/// Gets or sets the completion token associated with this message.
		/// </summary>
		public required CompletionToken CompletionToken { get; init; }

		/// <summary>
		/// Gets a value indicating whether the message has been completed (e.g. not streaming).
		/// This means this message has finished streaming or been loaded from database.
		/// </summary>
		public bool IsCompleted => CompletionToken.IsCompleted;

		public CompletionToken GetAwaiter() => CompletionToken;

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
				foreach (var toolCall in ToolCalls)
					toolCall.Dispose();
		}
	}
}