using LLMDesktopAssistant.SourceGenerators;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// The settings that related to tools implementations in the chat application.
	/// </summary>
	[SettingsRoute(nameof(ChatSettings.Tools))]
	public partial class ChatToolSettings : ChatSettingsCategoryBase
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

		private MetaToolSourcesSettings _sources = new();
		/// <summary>
		/// Gets or sets the meta tool sources group: the working directories search flag and the
		/// additional meta tool directories and files.
		/// </summary>
		[InheritedChatSetting]
		public MetaToolSourcesSettings Sources
		{
			get => _sources;
			set => SetProperty(ref _sources, value);
		}
	}
}
