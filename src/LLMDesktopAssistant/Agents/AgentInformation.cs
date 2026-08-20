namespace LLMDesktopAssistant.Agents
{
	public class AgentInformation : AgentSettingsCategoryBase
	{
		private string _name = string.Empty;
		/// <summary>
		/// The name of the agent.
		/// </summary>
		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		private string _description = string.Empty;
		/// <summary>
		/// The description of the agent.
		/// </summary>
		public string Description
		{
			get => _description;
			set => SetProperty(ref _description, value);
		}

		private string _base64ProfileImage = string.Empty;
		/// <summary>
		/// The base64 encoded profile image of the agent. If empty, no image is displayed.
		/// </summary>
		public string Base64ProfileImage
		{
			get => _base64ProfileImage;
			set => SetProperty(ref _base64ProfileImage, value);
		}

		private bool _identifyAsUser = false;
		/// <summary>
		/// When set, this agent identifies itself as a user to other agents.
		/// Its messages are treated as user messages (grouped into rounds, gated by user read permissions)
		/// and its tool calls and reasoning become inaccessible to other agents.
		/// </summary>
		public bool IdentifyAsUser
		{
			get => _identifyAsUser;
			set => SetProperty(ref _identifyAsUser, value);
		}
	}
}
