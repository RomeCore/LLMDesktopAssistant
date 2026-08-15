using LLMDesktopAssistant.SourceGenerators;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Chat-level skills settings: the local enable flag and the inheritable skill sources group.
	/// </summary>
	[SettingsRoute(nameof(ChatSettings.Skills))]
	public partial class ChatSkillSettings : ChatSettingsCategoryBase
	{
		private bool _enableSkills = true;
		/// <summary>
		/// Gets or sets a value indicating whether skills are enabled for the chat.
		/// </summary>
		public bool EnableSkills
		{
			get => _enableSkills;
			set => SetProperty(ref _enableSkills, value);
		}

		private SkillSourcesSettings _sources = new();
		/// <summary>
		/// Gets or sets the skill sources group: the working directories search flag and the
		/// additional skill directories and files.
		/// </summary>
		[InheritedChatSetting]
		public SkillSourcesSettings Sources
		{
			get => _sources;
			set => SetProperty(ref _sources, value);
		}
	}
}
