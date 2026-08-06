namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Represents the exposure group for an agent: what parts of this agent's messages
	/// are visible to other agents.
	/// </summary>
	public class AgentExposureSettings : NotifyPropertyChanged
	{
		private AgentExposureMode _exposureMode =
			AgentExposureMode.Reasoning |
			AgentExposureMode.Content |
			AgentExposureMode.ToolCalls |
			AgentExposureMode.Attachments |
			AgentExposureMode.MessagesWithToolCalls;
		/// <summary>
		/// The exposure mode that determines what parts of this agent's messages
		/// are visible to other agents.
		/// </summary>
		public AgentExposureMode ExposureMode
		{
			get => _exposureMode;
			set => SetProperty(ref _exposureMode, value);
		}
	}
}
