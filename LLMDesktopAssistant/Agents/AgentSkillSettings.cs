using LLMDesktopAssistant.Prompting.Skills;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents
{
	public class AgentSkillSettings : NotifyPropertyChanged
	{
		private bool _enableSkills = true;
		/// <summary>
		/// Gets or sets a value indicating whether skills are enabled for current agent.
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
		public RangeObservableCollection<SkillChange> SkillChanges
		{
			get => _skillChanges;
			set => _skillChanges.Reset(value);
		}
	}
}
