using System;
using System.Collections.Generic;
using System.Text;
using LLMDesktopAssistant.Utils;
using RCLargeLanguageModels.Tasks;

namespace LLMDesktopAssistant.Agents.Tasks
{
	/// <summary>
	/// Represents a single AI agent execution task.
	/// </summary>
	public class AgentTask : NotifyPropertyChanged
	{
		/// <summary>
		/// The parameters used to launch this agentic task.
		/// </summary>
		public required AgentTaskLaunchParameters LaunchParameters { get; init; }

		/// <summary>
		/// The completion token used to await the completion of this agentic task.
		/// </summary>
		public required CompletionToken Completion { get; init; }

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
