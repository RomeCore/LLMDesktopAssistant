namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Represents the context group for an agent: how many rounds are visible
	/// and whether context shields and summaries are allowed.
	/// </summary>
	public class AgentContextSettings : NotifyPropertyChanged
	{
		private int _maxVisibleRounds = 0;
		/// <summary>
		/// The maximum number of rounds that the agent can see in its context. If zero, there is no limit.
		/// </summary>
		public int MaxVisibleRounds
		{
			get => _maxVisibleRounds;
			set => SetProperty(ref _maxVisibleRounds, value);
		}

		private bool _allowContextShields = true;
		/// <summary>
		/// Whether the agent can use context shields to prevent seeing messages after shields.
		/// </summary>
		public bool AllowContextShields
		{
			get => _allowContextShields;
			set => SetProperty(ref _allowContextShields, value);
		}

		private bool _allowSummaries = true;
		/// <summary>
		/// Whether the agent is allowed to see summaries of messages in chat history and stop on them.
		/// </summary>
		public bool AllowSummaries
		{
			get => _allowSummaries;
			set => SetProperty(ref _allowSummaries, value);
		}
	}
}
