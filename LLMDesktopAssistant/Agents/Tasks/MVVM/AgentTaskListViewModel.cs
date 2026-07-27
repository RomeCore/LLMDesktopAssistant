using System.Collections.Specialized;
using Avalonia.Layout;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2021.DocumentTasks;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents.Tasks.MVVM
{
	/// <summary>
	/// View model for a list of <see cref="AgentTaskViewModel"/> items.
	/// Can be bound to an observable collection of <see cref="AgentTask"/> models
	/// and keeps the view-model list in sync automatically.
	/// </summary>
	[ViewModelFor(typeof(AgentTaskListView))]
	public class AgentTaskListViewModel : ViewModelBase
	{
		private readonly RangeObservableCollection<AgentTaskViewModel> _tasks = [];
		private readonly Dictionary<AgentTask, AgentTaskViewModel> _taskMap = [];
		/// <summary>
		/// The observable collection of task view models.
		/// </summary>
		public RangeObservableCollection<AgentTaskViewModel> Tasks => _tasks;

		private Orientation _orientation = Orientation.Vertical;
		/// <summary>
		/// The orientation of the list. Can be either vertical or horizontal.
		/// </summary>
		public Orientation Orientation
		{
			get => _orientation;
			set => SetProperty(ref _orientation, value);
		}

		private bool _hasTasks;
		/// <summary>
		/// Whether the list contains any tasks.
		/// </summary>
		public bool HasTasks
		{
			get => _hasTasks;
			private set => SetProperty(ref _hasTasks, value);
		}

		private int _runningTaskCount;
		/// <summary>
		/// The number of currently running (pending or executing) tasks.
		/// </summary>
		public int RunningTaskCount
		{
			get => _runningTaskCount;
			private set => SetProperty(ref _runningTaskCount, value);
		}

		private bool _hasRunningTasks;
		/// <summary>
		/// Whether the list contains any running tasks.
		/// </summary>
		public bool HasRunningTasks
		{
			get => _hasRunningTasks;
			private set => SetProperty(ref _hasRunningTasks, value);
		}

		private INotifyCollectionChanged? _sourceCollection;

		/// <summary>
		/// Binds this list to an observable collection of <see cref="AgentTask"/> models.
		/// </summary>
		/// <param name="source">An observable collection of <see cref="AgentTask"/>.</param>
		public void BindToSource(INotifyCollectionChanged source)
		{
			UnbindFromSource();

			_sourceCollection = source;

			if (source is IEnumerable<AgentTask> enumerable)
				foreach (var task in enumerable)
					AddTask(task);

			HasTasks = _tasks.Count > 0;
			RecalculateRunningCount();
			source.CollectionChanged += OnSourceCollectionChanged;
		}

		/// <summary>
		/// Unbinds from the current source collection and clears all tasks.
		/// </summary>
		public void UnbindFromSource()
		{
			_sourceCollection?.CollectionChanged -= OnSourceCollectionChanged;
			_sourceCollection = null;

			ClearTasks();

			HasTasks = false;
			RunningTaskCount = 0;
			HasRunningTasks = false;
		}

		private void AddTask(AgentTask task)
		{
			// Do not add task if it has parent (already added to parent task view model)
			if (task.Parent != null)
				return;
			if (_taskMap.ContainsKey(task))
				return;

			var vm = new AgentTaskViewModel(task);
			vm.PropertyChanged += OnTaskViewModelPropertyChanged;
			_taskMap[task] = vm;
			_tasks.Add(vm);
		}

		private void RemoveTask(AgentTask task)
		{
			if (!_taskMap.TryGetValue(task, out var vm))
				return;

			vm.Dispose();
			vm.PropertyChanged -= OnTaskViewModelPropertyChanged;
			_taskMap.Remove(task);
			_tasks.Remove(vm);
		}

		private void ClearTasks()
		{
			foreach (var vm in _tasks)
			{
				vm.PropertyChanged -= OnTaskViewModelPropertyChanged;
				vm.Dispose();
			}
			_taskMap.Clear();
			_tasks.Clear();
		}

		private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			InvokeUI(() =>
			{
				if (e.NewItems != null)
					foreach (AgentTask task in e.NewItems)
						AddTask(task);

				if (e.OldItems != null)
					foreach (AgentTask task in e.OldItems)
						RemoveTask(task);

				if (e.Action == NotifyCollectionChangedAction.Reset)
				{
					ClearTasks();

					if (_sourceCollection is IEnumerable<AgentTask> enumerable)
						foreach (var task in enumerable)
							AddTask(task);
				}

				HasTasks = _tasks.Count > 0;
				RecalculateRunningCount();
			});
		}

		private void OnTaskViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(AgentTaskViewModel.IsRunning))
				InvokeUI(RecalculateRunningCount);
		}

		private void RecalculateRunningCount()
		{
			var count = _tasks.Count(vm => vm.IsRunning);
			RunningTaskCount = count;
			HasRunningTasks = count > 0;
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing)
				UnbindFromSource();

			base.Dispose(disposing);
		}
	}
}
