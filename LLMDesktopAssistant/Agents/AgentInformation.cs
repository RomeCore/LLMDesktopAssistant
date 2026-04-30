namespace LLMDesktopAssistant.Agents
{
	public class AgentInformation : NotifyPropertyChanged
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

		private string _base64ProfileImage = string.Empty;
		/// <summary>
		/// The base64 encoded profile image of the agent. If empty, no image is displayed.
		/// </summary>
		public string Base64ProfileImage
		{
			get => _base64ProfileImage;
			set => SetProperty(ref _base64ProfileImage, value);
		}
	}
}
