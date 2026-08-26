using LLMDesktopAssistant.Prompting.Skills;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents
{
	public class SkillsetSettings : NotifyPropertyChanged
	{
		private bool _skillsEnabledByDefault = true;
		/// <summary>
		/// Gets or sets a value indicating whether unchanged skills are enabled by default.
		/// </summary>
		public bool SkillsEnabledByDefault
		{
			get => _skillsEnabledByDefault;
			set => SetProperty(ref _skillsEnabledByDefault, value);
		}

		private readonly RangeObservableCollection<SkillChange> _skillChanges = [];
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
