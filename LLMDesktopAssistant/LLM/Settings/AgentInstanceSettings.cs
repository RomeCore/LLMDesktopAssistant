namespace LLMDesktopAssistant.LLM.Settings
{
	public class AgentInstanceSettings : NotifyPropertyChanged
	{
		private Guid _agentId;
		/// <summary>
		/// Unique identifier for the agent.
		/// </summary>
		public Guid AgentId
		{
			get => _agentId;
			set => SetProperty(ref _agentId, value);
		}

		private bool _enabled = true;
		/// <summary>
		/// Whether the agent is enabled or not.
		/// </summary>
		public bool Enabled
		{
			get => _enabled;
			set => SetProperty(ref _enabled, value);
		}
	}
}