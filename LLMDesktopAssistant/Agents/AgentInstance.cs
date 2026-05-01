namespace LLMDesktopAssistant.Agents
{
	public class AgentInstance : NotifyPropertyChanged
	{
		private Guid _agentId;
		public Guid AgentId
		{
			get => _agentId;
			set => SetProperty(ref _agentId, value);
		}

		private bool _enabled = true;
		public bool Enabled
		{
			get => _enabled;
			set => SetProperty(ref _enabled, value);
		}
	}
}
