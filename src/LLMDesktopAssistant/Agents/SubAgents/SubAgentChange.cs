namespace LLMDesktopAssistant.Agents.SubAgents
{
	/// <summary>
	/// Represents a change applied to a sub-agent compared to the available sub-agent definitions.
	/// </summary>
	public class SubAgentChange : NotifyPropertyChanged
	{
		private string _subAgentName = string.Empty;
		/// <summary>
		/// The name of the sub-agent being changed.
		/// </summary>
		public string SubAgentName
		{
			get => _subAgentName;
			set => SetProperty(ref _subAgentName, value);
		}

		private bool? _enabled;
		/// <summary>
		/// Whether the sub-agent is enabled or not. Null indicates that the setting has not been changed yet.
		/// </summary>
		public bool? Enabled
		{
			get => _enabled;
			set => SetProperty(ref _enabled, value);
		}

		private string? _model;
		/// <summary>
		/// The model override for the sub-agent. Null indicates that the setting has not been changed yet.
		/// </summary>
		public string? Model
		{
			get => _model;
			set => SetProperty(ref _model, value);
		}
	}
}
