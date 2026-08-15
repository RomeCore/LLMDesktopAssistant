using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents.Tasks
{
	public interface IAgentTaskDispatcher
	{
		/// <summary>
		/// Gets all tasks that are currently being executed.
		/// </summary>
		ReadOnlyObservableCollection<AgentTask> AllTasks { get; }

		void OnBeginTask(AgentTask task);

		void OnEndTask(AgentTask task);
	}
}
