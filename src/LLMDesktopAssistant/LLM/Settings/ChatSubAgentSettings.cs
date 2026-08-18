using LLMDesktopAssistant.SourceGenerators;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Chat-level sub-agent settings: the local enable flag and the inheritable sub-agent sources group.
	/// </summary>
	[SettingsRoute(nameof(ChatSettings.SubAgents))]
	public partial class ChatSubAgentSettings : ChatSettingsCategoryBase
	{
		private bool _enableSubAgents = true;
		/// <summary>
		/// Gets or sets a value indicating whether sub-agents are enabled for the chat.
		/// </summary>
		public bool EnableSubAgents
		{
			get => _enableSubAgents;
			set => SetProperty(ref _enableSubAgents, value);
		}

		private SubAgentSourcesSettings _sources = new();
		/// <summary>
		/// Gets or sets the sub-agent sources group: the working directories search flag and the
		/// additional sub-agent directories and files.
		/// </summary>
		[InheritedChatSetting]
		public SubAgentSourcesSettings Sources
		{
			get => _sources;
			set => SetProperty(ref _sources, value);
		}
	}
}
