using System.Collections.Specialized;
using LLMDesktopAssistant.Agents.Tasks;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Messages;
using Material.Icons;

namespace LLMDesktopAssistant.UIExtensions.MessageExtensions
{
	/// <summary>
	/// A message extension that synchronizes <see cref="AssistantMessage.AgentTasks"/>
	/// with <see cref="ChatMessage.AdditionalViewModels"/>, displaying inline
	/// <see cref="AgentTaskViewModel"/> cards below the assistant message.
	/// </summary>
	/// <remarks>
	/// This extension is invisible (it does not produce a toolbar button) —
	/// its sole purpose is to subscribe to task changes and manage the
	/// additional view models automatically.
	/// </remarks>
	[MessageExtension(Targets = MessageExtensionTargets.Assistant)]
	public class AgentTaskMessageExtension : MessageExtension
	{
		private AssistantMessage? _message;

		/// <summary>
		/// Initializes a new instance of the <see cref="AgentTaskMessageExtension"/> class.
		/// </summary>
		/// <param name="viewModel">The assistant message view model.</param>
		public AgentTaskMessageExtension(MessageViewModelBase viewModel)
		{
			IsVisible = false; // No toolbar button — inline cards only

			if (viewModel is not AssistantMessageViewModel assistantVm)
				return;

			_message = assistantVm.AssistantMessage;

			// Add existing tasks
			foreach (var task in _message.AgentTasks)
				AddTaskToAdditionalViewModels(task);

			// Subscribe to future changes
			_message.AgentTasks.CollectionChanged += OnAgentTasksChanged;
		}

		private void OnAgentTasksChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (_message == null) return;
			if (e.NewItems != null)
			{
				foreach (AgentTask task in e.NewItems)
					AddTaskToAdditionalViewModels(task);
			}

			if (e.OldItems != null)
			{
				foreach (AgentTask task in e.OldItems)
					RemoveTaskViewModel(task);
			}
		}

		private void AddTaskToAdditionalViewModels(AgentTask task)
		{
			var taskVm = new Agents.Tasks.MVVM.AgentTaskViewModel(task);
			var additional = new AgentTaskInlineViewModel
			{
				TaskViewModel = taskVm
			};
			_message.AdditionalViewModels.Add(additional);
		}

		private void RemoveTaskViewModel(AgentTask task)
		{
			var toRemove = _message.AdditionalViewModels
				.GetAll<AgentTaskInlineViewModel>()
				.FirstOrDefault(vm => vm.TaskViewModel.Task == task);

			if (toRemove != null)
			{
				toRemove.TaskViewModel.Dispose();
				_message.AdditionalViewModels.Remove(toRemove);
			}
		}

		private void RemoveAllTaskViewModels()
		{
			var toRemove = _message.AdditionalViewModels
				.GetAll<AgentTaskInlineViewModel>()
				.ToList();

			foreach (var vm in toRemove)
			{
				vm.TaskViewModel.Dispose();
				_message.AdditionalViewModels.Remove(vm);
			}
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing && _message != null)
			{
				_message.AgentTasks.CollectionChanged -= OnAgentTasksChanged;
				RemoveAllTaskViewModels();
			}

			base.Dispose(disposing);
		}
	}
}
