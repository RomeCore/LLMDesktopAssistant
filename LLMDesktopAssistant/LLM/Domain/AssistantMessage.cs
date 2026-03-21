using System.Collections.ObjectModel;

namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents a message from the LLM assistant.
	/// </summary>
	public class AssistantMessage : ChatMessage
	{
		private string? _reasoningContent = null;
		/// <summary>
		/// Gets or sets the reasoning content of the message.
		/// </summary>
		public string? ReasoningContent
		{
			get => _reasoningContent;
			set => SetProperty(ref _reasoningContent, value);
		}

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
		/// The collection of tool calls associated with this message. If no tool calls were made, this property is an empty collection.
		/// </summary>
		public ObservableCollection<ToolCall> ToolCalls { get; } = [];
	}
}