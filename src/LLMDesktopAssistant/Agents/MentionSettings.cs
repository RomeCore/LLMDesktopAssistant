namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Represents the mention group of the agent execution conditions: whether the agent
	/// can be mentioned by other agents and users, and whether it can mention other agents.
	/// </summary>
	public class MentionSettings : NotifyPropertyChanged
	{
		private bool _canBeMentioned = true;
		/// <summary>
		/// Whether the agent can be mentioned by other agents and users, causing the agent to execute.
		/// </summary>
		public bool CanBeMentioned
		{
			get => _canBeMentioned;
			set => SetProperty(ref _canBeMentioned, value);
		}

		private bool _canMentionOthers = true;
		/// <summary>
		/// Whether the agent can mention other agents, causing them to be executed.
		/// </summary>
		public bool CanMentionOthers
		{
			get => _canMentionOthers;
			set => SetProperty(ref _canMentionOthers, value);
		}
	}
}
