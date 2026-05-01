namespace LLMDesktopAssistant.Agents
{
	public class WeightedAgentInstance : AgentInstance
	{
		private double _weight = 1.0;
		public double Weight
		{
			get => _weight;
			set => SetProperty(ref _weight, value);
		}
	}
}
