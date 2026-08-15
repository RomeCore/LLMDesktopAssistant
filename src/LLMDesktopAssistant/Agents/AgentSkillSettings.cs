using LLMDesktopAssistant.Prompting.Skills;
using LLMDesktopAssistant.SourceGenerators;
using LLMDesktopAssistant.Utils;

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

		private RangeObservableCollection<SkillChange> _skillChanges = [];
		/// <summary>
		/// Gets or sets the skill changes compared to all available skills.
		/// </summary>
		[InheritedChatAgentSetting]
		public RangeObservableCollection<SkillChange> SkillChanges
		{
			get => _skillChanges;
			set => _skillChanges.Reset(value);
		}
	}
}
