using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents.Tasks
{
	public class AgentAssistantMessage : AgentChatMessage
	{
		private string? _reasoningContent;
		/// <summary>
		/// The reasoning content of the assistant's message.
		/// </summary>
		public string? ReasoningContent
		{
			get => _reasoningContent;
			set => SetProperty(ref _reasoningContent, value);
		}

		private RangeObservableCollection<AgentAttachment> _attachments = [];
		/// <summary>
		/// A collection of attachments associated with the assistant's message.
		/// </summary>
		public RangeObservableCollection<AgentAttachment> Attachments => _attachments;

		private RangeObservableCollection<AgentToolCall> _toolCalls = [];
		/// <summary>
		/// A collection of tool calls associated with the assistant's message.
		/// </summary>
		public RangeObservableCollection<AgentToolCall> ToolCalls => _toolCalls;
	}
}
