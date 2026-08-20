using System.Collections.ObjectModel;
using LLMDesktopAssistant.Agents.Tasks;
using LLMDesktopAssistant.Utils;
using RCLargeLanguageModels.Tasks;

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

		/// <summary>
		/// When set, this message is treated as a user message by other agents:
		/// it participates in round grouping as a user message, is gated by user read permissions
		/// and is rendered using the user message template. Set at creation time based on
		/// <see cref="AgentInformation.IdentifyAsUser"/> and never changes afterwards.
		/// </summary>
		public bool IsUserLike { get; init; }

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
		/// Gets the collection of agent tasks associated with this message.
		/// </summary>
		public RangeObservableCollection<AgentTask> AgentTasks { get; } = [];

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