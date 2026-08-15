using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Represents the skill sources group of a chat: the flag controlling whether skills are
	/// fetched from all working directories, and the lists of additional skill directories
	/// and skill files.
	/// </summary>
	public class SkillSourcesSettings : NotifyPropertyChanged
	{
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

		private readonly RangeObservableCollection<string> _additionalSkillDirectories = [];
		/// <summary>
		/// Gets or sets the additional skill directories. These are paths similar to <c>skills/</c> directories.
		/// Example for additional directory <c>C:/MyProject/.claude/skills/</c>: <c>C:/MyProject/.claude/skills/my_skill/SKILL.md</c>
		/// </summary>
		public RangeObservableCollection<string> AdditionalSkillDirectories
		{
			get => _additionalSkillDirectories;
			set => _additionalSkillDirectories.Reset(value);
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
