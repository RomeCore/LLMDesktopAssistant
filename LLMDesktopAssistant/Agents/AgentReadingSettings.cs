namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Represents the reading permissions group for an agent: what the agent can read.
	/// </summary>
	public class AgentReadingSettings : NotifyPropertyChanged
	{
		private AgentReadPermissions _readPermissions =
			AgentReadPermissions.UserMessages |
			AgentReadPermissions.UserAttachments |
			AgentReadPermissions.OwnMessages |
			AgentReadPermissions.OtherAgentMessages |
			AgentReadPermissions.OtherAgentContent |
			AgentReadPermissions.OtherAgentToolCalls |
			AgentReadPermissions.OtherAgentAttachments |
			AgentReadPermissions.MessagesWithToolCalls;
		/// <summary>
		/// The permissions that determine what the agent can read.
		/// </summary>
		public AgentReadPermissions ReadPermissions
		{
			get => _readPermissions;
			set => SetProperty(ref _readPermissions, value);
		}
	}
}
