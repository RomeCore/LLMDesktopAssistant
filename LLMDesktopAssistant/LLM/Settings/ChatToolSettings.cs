using LLMDesktopAssistant.Tools;

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


		private ToolBehaviour _autoApproveBehaviours = ToolBehaviour.None;
		/// <summary>
		/// The behaviour of tools that will be automatically approved.
		/// </summary>
		public ToolBehaviour AutoApproveBehaviours
		{
			get => _autoApproveBehaviours;
			set => SetProperty(ref _autoApproveBehaviours, value);
		}

		private ToolBehaviour _disallowedBehaviours = ToolBehaviour.None;
		/// <summary>
		/// The behaviour of tools that will be disallowed.
		/// </summary>
		public ToolBehaviour DisallowedBehaviours
		{
			get => _disallowedBehaviours;
			set => SetProperty(ref _disallowedBehaviours, value);
		}

	}
}