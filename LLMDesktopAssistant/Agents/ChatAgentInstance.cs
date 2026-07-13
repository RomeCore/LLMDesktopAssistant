namespace LLMDesktopAssistant.Agents
{
	public class ChatAgentInstance : NotifyPropertyChanged
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

		private double _weight = 1.0;
		public double Weight
		{
			get => _weight;
			set => SetProperty(ref _weight, value);
		}
	}
}
