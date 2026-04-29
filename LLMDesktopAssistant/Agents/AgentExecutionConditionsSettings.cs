namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Describes an agent execution conditions settings.
	/// </summary>
	public class AgentExecutionConditionsSettings : NotifyPropertyChanged
	{
		private int _order = 0;
		/// <summary>
		/// The default order of agent in the chat. Lower numbers are executed first.
		/// </summary>
		public int Order
		{
			get => _order;
			set => SetProperty(ref _order, value);
		}

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