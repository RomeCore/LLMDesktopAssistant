using LLMDesktopAssistant.SourceGenerators;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Chat-level skills settings.
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
	}
}
