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

		private readonly RangeObservableCollection<AgentToolCallConfirmationRequest> _toolCallConfirmationRequests = [];
		/// <summary>
		/// A collection of tool calls that require user confirmation before execution.
		/// </summary>
		public RangeObservableCollection<AgentToolCallConfirmationRequest> ToolCallConfirmationRequests => _toolCallConfirmationRequests;
	}
}
