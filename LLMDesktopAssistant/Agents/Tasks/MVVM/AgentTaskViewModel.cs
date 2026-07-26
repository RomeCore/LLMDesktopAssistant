using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.Utils;
using Avalonia.Media;
using LLMDesktopAssistant.Localization;
using Material.Icons;

namespace LLMDesktopAssistant.Agents.Tasks.MVVM
{
	/// <summary>
	/// View model for a single <see cref="AgentTask"/>, providing UI-friendly bindings
	/// for status, statistics, sub-tasks, and cancellation.
	/// </summary>
	[ViewModelFor(typeof(AgentTaskView))]
	public class AgentTaskViewModel : ViewModelBase
	{
		private readonly AgentTask _task;

		/// <summary>
		/// The underlying <see cref="AgentTask"/> model.
		/// </summary>
		public AgentTask Task => _task;

		/// <summary>
		/// The display name of the task.
		/// </summary>
		public string? TaskName => _task.LaunchParameters.TaskName;

		private MaterialIconKind _statusIcon = MaterialIconKind.ClockOutline;
		/// <summary>
		/// The icon representing the current status of the task.
		/// </summary>
		public MaterialIconKind StatusIcon
		{
			get => _statusIcon;
			private set => SetProperty(ref _statusIcon, value);
		}

		private string _statusText = string.Empty;
		/// <summary>
		/// Localized text describing the current status of the task.
		/// </summary>
		public string StatusText
		{
			get => _statusText;
			private set => SetProperty(ref _statusText, value);
		}

		private bool _isRunning;
		/// <summary>
		/// Whether the task is currently running (pending or executing).
		/// </summary>
		public bool IsRunning
		{
			get => _isRunning;
			private set => SetProperty(ref _isRunning, value);
		}

		private bool _isCompleted;
		/// <summary>
		/// Whether the task has reached a terminal state.
		/// </summary>
		public bool IsCompleted
		{
			get => _isCompleted;
			private set => SetProperty(ref _isCompleted, value);
		}

		private IBrush? _statusBackground;
		/// <summary>
		/// Gets the background brush for the task card, based on its current status.
		/// </summary>
		public IBrush? StatusBackground
		{
			get => _statusBackground;
			private set => SetProperty(ref _statusBackground, value);
		}

		private string? _summary;
		/// <summary>
		/// A compact summary string (execution time, token count).
		/// </summary>
		public string? Summary
		{
			get => _summary;
			private set => SetProperty(ref _summary, value);
		}

		private string? _lastContent;
		/// <summary>
		/// The last generated text content from the assistant.
		/// </summary>
		public string? LastContent
		{
			get => _lastContent;
			private set => SetProperty(ref _lastContent, value);
		}

		private bool _isExpanded;
		/// <summary>
		/// Whether the sub-tasks tree is expanded in the UI.
		/// </summary>
		public bool IsExpanded
		{
			get => _isExpanded;
			set => SetProperty(ref _isExpanded, value);
		}

		private bool _hasSubTasks;
		/// <summary>
		/// Whether the task contains any sub-tasks.
		/// </summary>
		public bool HasSubTasks
		{
			get => _hasSubTasks;
			private set => SetProperty(ref _hasSubTasks, value);
		}

		/// <summary>
		/// View models for the sub-tasks of this task.
		/// </summary>
		public RangeObservableCollection<AgentTaskViewModel> SubTaskViewModels { get; } = [];

		/// <summary>
		/// Command to cancel the task.
		/// </summary>
		public ICommand CancelCommand { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="AgentTaskViewModel"/> class.
		/// </summary>
		/// <param name="task">The <see cref="AgentTask"/> to wrap.</param>
		public AgentTaskViewModel(AgentTask task)
		{
			_task = task;
			CancelCommand = new RelayCommand(() => _task.CancellationTokenSource.Cancel(), () => IsRunning);

			SyncStatus();
			SyncSummary();
			SyncSubTasks();

			_task.PropertyChanged += OnTaskPropertyChanged;
			_task.SubTasks.CollectionChanged += OnSubTasksCollectionChanged;
		}

		private void OnTaskPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			InvokeUI(() =>
			{
				switch (e.PropertyName)
				{
					case nameof(AgentTask.Status):
						SyncStatus();
						UpdateCancelCommand();
						break;

					case nameof(AgentTask.Completed):
						SyncStatus();
						UpdateCancelCommand();
						break;

					case nameof(AgentTask.LastGeneratedContent):
						LastContent = _task.LastGeneratedContent;
						break;

					case nameof(AgentTask.UsageStatistics):
						SyncSummary();
						break;
				}
			});
		}

		private void OnSubTasksCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			InvokeUI(() =>
			{
				if (e.NewItems != null)
				{
					foreach (AgentTask subTask in e.NewItems)
						SubTaskViewModels.Add(new AgentTaskViewModel(subTask));
				}

				if (e.OldItems != null)
				{
					foreach (AgentTask subTask in e.OldItems)
					{
						var toRemove = SubTaskViewModels.FirstOrDefault(vm => vm.Task == subTask);
						if (toRemove != null)
						{
							toRemove.Dispose();
							SubTaskViewModels.Remove(toRemove);
						}
					}
				}

				HasSubTasks = SubTaskViewModels.Count > 0;
			});
		}

		private void SyncStatus()
		{
			IsCompleted = _task.Completed;
			IsRunning = _task.Status is AgentTaskStatus.Pending or AgentTaskStatus.Executing;

			(StatusIcon, StatusText, StatusBackground) = _task.Status switch
			{
				AgentTaskStatus.Pending => (MaterialIconKind.ClockOutline,
					LocalizationManager.LocalizeStatic("task_status_pending"), (IBrush?)null),
				AgentTaskStatus.Executing => (MaterialIconKind.TimerSandComplete,
					LocalizationManager.LocalizeStatic("task_status_executing"),
					new SolidColorBrush(Color.FromArgb(0x1A, 0x4C, 0xAF, 0x50))),
				AgentTaskStatus.Success => (MaterialIconKind.CheckCircle,
					LocalizationManager.LocalizeStatic("task_status_success"),
					new SolidColorBrush(Color.FromArgb(0x0A, 0x4C, 0xAF, 0x50))),
				AgentTaskStatus.Failed => (MaterialIconKind.AlertCircle,
					LocalizationManager.LocalizeStatic("task_status_failed"),
					new SolidColorBrush(Color.FromArgb(0x1A, 0xF4, 0x43, 0x36))),
				AgentTaskStatus.Cancelled => (MaterialIconKind.Cancel,
					LocalizationManager.LocalizeStatic("task_status_cancelled"),
					new SolidColorBrush(Color.FromArgb(0x1A, 0xFF, 0x98, 0x00))),
				_ => (MaterialIconKind.Help,
					LocalizationManager.LocalizeStatic("task_status_unknown"), (IBrush?)null)
			};
		}

		private void SyncSummary()
		{
			if (_task.UsageStatistics is { } stats)
			{
				var time = stats.ExecutionTime.TotalSeconds >= 1.0
					? $"{stats.ExecutionTime.TotalSeconds:F1}s"
					: $"{stats.ExecutionTime.TotalMilliseconds:F0}ms";

				Summary = $"⏱ {time}  ⬆{stats.InputTokens} ⬇{stats.OutputTokens}";
			}
			else
			{
				Summary = null;
			}
		}

		private void SyncSubTasks()
		{
			foreach (var subTask in _task.SubTasks)
				SubTaskViewModels.Add(new AgentTaskViewModel(subTask));

			HasSubTasks = SubTaskViewModels.Count > 0;
		}

		private void UpdateCancelCommand()
		{
			InvokeUI(() => ((RelayCommand)CancelCommand).NotifyCanExecuteChanged());
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_task.PropertyChanged -= OnTaskPropertyChanged;
				_task.SubTasks.CollectionChanged -= OnSubTasksCollectionChanged;

				foreach (var subVm in SubTaskViewModels)
					subVm.Dispose();
				SubTaskViewModels.Clear();
			}

			base.Dispose(disposing);
		}
	}
}
