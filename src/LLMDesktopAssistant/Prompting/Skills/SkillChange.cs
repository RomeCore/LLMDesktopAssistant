namespace LLMDesktopAssistant.Prompting.Skills
{
	public class SkillChange : NotifyPropertyChanged
	{
		private string _skillName = string.Empty;
		/// <summary>
		/// The name of the skill being changed.
		/// </summary>
		public string SkillName
		{
			get => _skillName;
			set => SetProperty(ref _skillName, value);
		}

		private bool? _enabled;
		/// <summary>
		/// Whether the skill is enabled or not. Null indicates that the setting has not been changed yet.
		/// </summary>
		public bool? Enabled
		{
			get => _enabled;
			set => SetProperty(ref _enabled, value);
		}

		private SkillInjectionMode? _injectionMode;
		/// <summary>
		/// The mode for injecting the skill into the prompt. Null indicates that the setting has not been changed yet.
		/// </summary>
		public SkillInjectionMode? InjectionMode
		{
			get => _injectionMode;
			set => SetProperty(ref _injectionMode, value);
		}
	}
}
