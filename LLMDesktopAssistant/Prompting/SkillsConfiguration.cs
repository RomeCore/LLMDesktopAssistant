using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Prompting
{
	public class SkillsConfiguration : SettingsObject
	{
		private readonly RangeObservableCollection<SkillPrompt> _skills = [];
		/// <summary>
		/// Gest or sets the list of skills.
		/// </summary>
		public ICollection<SkillPrompt> Skills
		{
			get => _skills;
			set => _skills.Reset(value);
		}
	}
}
