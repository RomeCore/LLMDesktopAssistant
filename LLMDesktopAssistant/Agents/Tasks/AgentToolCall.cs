using LLMDesktopAssistant.Tools;
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

		/// <summary>
		/// The tool call ID. This is a unique identifier for the tool call.
		/// </summary>
		public required string ToolCallId { get; init; }

		/// <summary>
		/// The name/identifier of the tool being called.
		/// </summary>
		public required string ToolName { get; init; }

		private string _arguments = string.Empty;
		/// <summary>
		/// The arguments passed to the tool.
		/// </summary>
		public string Arguments
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

		private ToolBehaviour _expectedBehaviour;
		/// <summary>
		/// The expected behaviour flags for this tool call, determined during pre-execution.
		/// Used to display behaviour indicators in the confirmation UI.
		/// </summary>
		public ToolBehaviour ExpectedBehaviour
		{
			get => _expectedBehaviour;
			set => SetProperty(ref _expectedBehaviour, value);
		}
	}
}
