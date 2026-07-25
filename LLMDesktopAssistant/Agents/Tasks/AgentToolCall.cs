using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents.Tasks
{

	public class AgentToolCall : NotifyPropertyChanged
	{
		private AgentToolCallStatus _status = AgentToolCallStatus.Pending;
		/// <summary>
		/// Gets or sets the status of the tool call.
		/// </summary>
		public AgentToolCallStatus Status
		{
			get => _status;
			set => SetProperty(ref _status, value);
		}

		private string _toolCallId = string.Empty;
		/// <summary>
		/// The tool call ID. This is a unique identifier for the tool call.
		/// </summary>
		public string ToolCallId
		{
			get => _toolCallId;
			set => SetProperty(ref _toolCallId, value);
		}

		private string _toolName = string.Empty;
		/// <summary>
		/// The name/identifier of the tool being called.
		/// </summary>
		public string ToolName
		{
			get => _toolName;
			set => SetProperty(ref _toolName, value);
		}

		private string? _arguments;
		/// <summary>
		/// The arguments passed to the tool.
		/// </summary>
		public string? Arguments
		{
			get => _arguments;
			set => SetProperty(ref _arguments, value);
		}

		private AgentToolCallResult? _result;
		/// <summary>
		/// The result of the tool call.
		/// </summary>
		public AgentToolCallResult? Result
		{
			get => _result;
			set => SetProperty(ref _result, value);
		}
	}
}
