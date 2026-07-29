using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.Utils;
using Material.Icons;

namespace LLMDesktopAssistant.Agents.Tasks.MVVM
{
	/// <summary>
	/// View model for the detailed agent task dialog. Provides an expanded,
	/// read-only view of all task data: messages, tool calls, sub-tasks,
	/// usage statistics, and error information.
	/// </summary>
	[ViewModelFor(typeof(AgentTaskDetailView))]
	public class AgentTaskDetailViewModel : ViewModelBase
	{
		private readonly AgentTaskViewModel _taskVm;

		/// <summary>
		/// The wrapped <see cref="AgentTaskViewModel"/> that this detail view is based on.
		/// </summary>
		public AgentTaskViewModel TaskViewModel => _taskVm;

		/// <summary>
		/// The underlying <see cref="AgentTask"/> model.
		/// </summary>
		public AgentTask Task => TaskViewModel.Task;

		/// <summary>
		/// The formatted execution time string.
		/// </summary>
		public string? ExecutionTimeFormatted => Task.UsageStatistics is { } stats
			? FormatTimeSpan(stats.ExecutionTime)
			: null;

		/// <summary>
		/// The formatted time to first token string.
		/// </summary>
		public string? TtftFormatted => Task.UsageStatistics is { } stats
			? FormatTimeSpan(stats.TimeToFirstToken)
			: null;

		/// <summary>
		/// The formatted inference time string.
		/// </summary>
		public string? InferenceTimeFormatted => Task.UsageStatistics is { } stats
			? FormatTimeSpan(stats.InferenceTime)
			: null;

		/// <summary>
		/// The formatted time out string.
		/// </summary>
		public string? TimeOut => Task.LaunchParameters.TimeOut is { } timeout
			? FormatTimeSpan(timeout)
			: null;

		/// <summary>
		/// Command to cancel the task.
		/// </summary>
		public ICommand CancelCommand => _taskVm.CancelCommand;

		/// <summary>
		/// Command to close the dialog.
		/// </summary>
		public ICommand CloseCommand { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="AgentTaskDetailViewModel"/> class.
		/// </summary>
		/// <param name="taskVm">The <see cref="AgentTaskViewModel"/> to display in detail.</param>
		public AgentTaskDetailViewModel(AgentTaskViewModel taskVm)
		{
			_taskVm = taskVm;
			CloseCommand = new RelayCommand(() => DialogManager.CloseDialog());

			Task.PropertyChanged += (_, e) =>
			{
				InvokeUI(() =>
				{
					switch (e.PropertyName)
					{
						case nameof(AgentTask.UsageStatistics):
							RefreshUsageProperties();
							break;
					}
				});
			};
		}

		private void RefreshUsageProperties()
		{
			RaisePropertyChanged(nameof(ExecutionTimeFormatted));
			RaisePropertyChanged(nameof(TtftFormatted));
			RaisePropertyChanged(nameof(InferenceTimeFormatted));
		}

		private static string FormatTimeSpan(TimeSpan ts)
		{
			if (ts.TotalMinutes >= 1)
				return $"{ts.TotalMinutes:F1} min";
			if (ts.TotalSeconds >= 1)
				return $"{ts.TotalSeconds:F1} s";
			return $"{ts.TotalMilliseconds:F0} ms";
		}
	}
}
