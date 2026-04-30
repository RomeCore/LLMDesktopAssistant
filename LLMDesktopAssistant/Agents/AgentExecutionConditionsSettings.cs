namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Describes an agent execution conditions settings.
	/// </summary>
	public class AgentExecutionConditionsSettings : NotifyPropertyChanged
	{
		private bool _canExecuteAgain = false;
		/// <summary>
		/// Whether the agent can execute again after it has already executed.
		/// </summary>
		public bool CanExecuteAgain
		{
			get => _canExecuteAgain;
			set => SetProperty(ref _canExecuteAgain, value);
		}

		private AgentExecutionChecker _executionChecker = AgentExecutionChecker.Always;
		/// <summary>
		/// The checker that determines when to execute the agent.
		/// </summary>
		public AgentExecutionChecker ExecutionChecker
		{
			get => _executionChecker;
			set => SetProperty(ref _executionChecker, value);
		}
	}
}