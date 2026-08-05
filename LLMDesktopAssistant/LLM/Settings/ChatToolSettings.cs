namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// The settings that related to tools implementations in the chat application.
	/// </summary>
	public class ChatToolSettings : ChatSettingsCategoryBase
	{
		private bool _enableTools = true;
		/// <summary>
		/// Whether to use tools in the chat.
		/// </summary>
		public bool EnableTools
		{
			get => _enableTools;
			set => SetProperty(ref _enableTools, value);
		}

		private bool _fetchFromAllWorkingDirectories = false;
		/// <summary>
		/// Gets or sets a value indicating whether meta tools should be fetched from all
		/// working directories (see <see cref="ChatEnvironmentSettings.WorkingDirectories"/>).
		/// </summary>
		public bool FetchFromAllWorkingDirectories
		{
			get => _fetchFromAllWorkingDirectories;
			set => SetProperty(ref _fetchFromAllWorkingDirectories, value);
		}
	}
}
