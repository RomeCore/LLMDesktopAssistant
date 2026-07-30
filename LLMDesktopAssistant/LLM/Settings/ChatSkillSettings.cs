using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	public class ChatSkillSettings : NotifyPropertyChanged
	{
		private bool _enableSkills = true;
		/// <summary>
		/// Gets or sets a value indicating whether skills are enabled.
		/// </summary>
		public bool EnableSkills
		{
			get => _enableSkills;
			set => SetProperty(ref _enableSkills, value);
		}

		private bool _fetchFromAllWorkingDirectories = false;
		/// <summary>
		/// Gets or sets a value indicating whether skills should be fetched from all
		/// working directories (see <see cref="ChatEnvironmentSettings.WorkingDirectories"/>).
		/// </summary>
		public bool FetchFromAllWorkingDirectories
		{
			get => _fetchFromAllWorkingDirectories;
			set => SetProperty(ref _fetchFromAllWorkingDirectories, value);
		}

		private readonly RangeObservableCollection<string> _additionalSkillPaths = [];
		/// <summary>
		/// Gets or sets the additional skill paths. These are paths similar to <c>skills/</c> directories.
		/// Example for additional directory <c>C:/MyProject/.claude/skills/</c>: <c>C:/MyProject/.claude/skills/my_skill/SKILL.md</c>
		/// </summary>
		public RangeObservableCollection<string> AdditionalSkillDirectories
		{
			get => _additionalSkillPaths;
			set => _additionalSkillPaths.Reset(value);
		}

		private readonly RangeObservableCollection<string> _additionalSkillFiles = [];
		/// <summary>
		/// Gets or sets the additional skill files. These are paths to individual SKILL.md files.
		/// </summary>
		public RangeObservableCollection<string> AdditionalSkillFiles
		{
			get => _additionalSkillFiles;
			set => _additionalSkillFiles.Reset(value);
		}
	}
}