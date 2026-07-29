namespace LLMDesktopAssistant.Agents.Tasks
{
	public interface IAgentTaskExecutor
	{
		/// <summary>
		/// Launches a new task with the given parameters.
		/// </summary>
		/// <param name="parameters">The parameters for the task.</param>
		/// <param name="cancellationToken">A token to cancel the task execution. </param>
		/// <returns>The task that can be tracked and awaited.</returns>
		AgentTask Execute(AgentTaskLaunchParameters parameters, CancellationToken cancellationToken = default);
	}
}
