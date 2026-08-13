using System.Runtime.CompilerServices;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents.Tasks
{
	/// <summary>
	/// Represents a single AI agent execution task.
	/// </summary>
	public class AgentTask : NotifyPropertyChanged
	{
		/// <summary>
		/// The unique identifier for this agentic task.
		/// </summary>
		public required Guid Id { get; init; }

		/// <summary>
		/// The parent of this agentic task, if any.
		/// </summary>
		public required AgentTask? Parent { get; init; }

		/// <summary>
		/// The parameters used to launch this agentic task.
		/// </summary>
		public required AgentTaskLaunchParameters LaunchParameters { get; init; }

		/// <summary>
		/// The completion token used to await the completion of this agentic task.
		/// </summary>
		public required Task<AgentTask> Completion { get; init; }
		public TaskAwaiter<AgentTask> GetAwaiter() => Completion.GetAwaiter();

		/// <summary>
		/// The cancellation token source used to cancel this agentic task.
		/// </summary>
		public required CancellationTokenSource CancellationTokenSource { get; init; }

		private readonly RangeObservableCollection<AgentTask> _subTasks = [];
		/// <summary>
		/// A collection of sub-tasks associated with this agentic task.
		/// </summary>
		public RangeObservableCollection<AgentTask> SubTasks => _subTasks;

		private readonly RangeObservableCollection<AgentChatMessage> _messages = [];
		/// <summary>
		/// A collection of chat messages associated with this agentic task.
		/// </summary>
		public RangeObservableCollection<AgentChatMessage> Messages => _messages;

		private AgentAssistantMessage? _lastGeneratedMessage;
		/// <summary>
		/// The last generated message by the agent associated with this task.
		/// </summary>
		public AgentAssistantMessage? LastGeneratedMessage
		{
			get => _lastGeneratedMessage;
			internal set => SetProperty(ref _lastGeneratedMessage, value);
		}

		private string? _lastGeneratedContent;
		/// <summary>
		/// The last generated message's content by the agent associated with this task.
		/// </summary>
		public string? LastGeneratedContent
		{
			get => _lastGeneratedContent;
			internal set => SetProperty(ref _lastGeneratedContent, value);
		}

		private AgentTaskStatus _status = AgentTaskStatus.Pending;
		/// <summary>
		/// The current status of this agentic task.
		/// </summary>
		public AgentTaskStatus Status
		{
			get => _status;
			internal set => SetProperty(ref _status, value);
		}

		private bool _completed;
		/// <summary>
		/// Indicates whether this agentic task has completed.
		/// </summary>
		public bool Completed
		{
			get => _completed;
			internal set => SetProperty(ref _completed, value);
		}

		private int _iterationCount;
		/// <summary>
		/// The number of tool call loop iterations completed by this task.
		/// </summary>
		public int IterationCount
		{
			get => _iterationCount;
			internal set => SetProperty(ref _iterationCount, value);
		}

		private Exception? _exception;
		/// <summary>
		/// The exception that occurred during the execution of this agentic task.
		/// </summary>
		public Exception? Exception
		{
			get => _exception;
			internal set => SetProperty(ref _exception, value);
		}

		private AgentUsageStatistics? _usageStatistics;
		/// <summary>
		/// The usage statistics for this agentic task.
		/// This is summary for all processed message's usage.
		/// </summary>
		public AgentUsageStatistics? UsageStatistics
		{
			get => _usageStatistics;
			set => SetProperty(ref _usageStatistics, value);
		}

		private readonly RangeObservableCollection<AgentToolCallConfirmationRequest> _toolCallConfirmationRequests = [];
		/// <summary>
		/// A collection of tool calls that require user confirmation before execution.
		/// </summary>
		public RangeObservableCollection<AgentToolCallConfirmationRequest> ToolCallConfirmationRequests => _toolCallConfirmationRequests;
	}
}
