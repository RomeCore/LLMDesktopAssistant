using LLMDesktopAssistant.SourceGenerators;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Describes the agent-level skills settings: the local enable flag and the inheritable
	/// list of per-skill changes.
	/// </summary>
	[SettingsRoute(nameof(ChatAgentDescriptor.Skills))]
	public partial class AgentSkillSettings : AgentSettingsCategoryBase
	{
		private bool _enableSkills = true;
		/// <summary>
		/// Gets or sets a value indicating whether skills are enabled for the agent.
		/// </summary>
		public bool EnableSkills
		{
			get => _enableSkills;
			set => SetProperty(ref _enableSkills, value);
		}

		/// <summary>
		/// Gets or sets the skillset settings for the agent.
		/// </summary>
		[InheritedChatAgentSetting]
		public SkillsetSettings Skillset
		{
			get => field ??= new();
			set => SetProperty(ref field, value);
		}
	}
}
