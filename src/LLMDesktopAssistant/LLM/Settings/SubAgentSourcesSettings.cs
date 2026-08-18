using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Represents the sub-agent sources group of a chat: the flag controlling whether sub-agents are
	/// fetched from all working directories, and the lists of additional sub-agent directories
	/// and sub-agent files.
	/// </summary>
	public class SubAgentSourcesSettings : NotifyPropertyChanged
	{
		private bool _fetchFromAllWorkingDirectories = false;
		/// <summary>
		/// Gets or sets a value indicating whether sub-agents should be fetched from all
		/// working directories (see <see cref="ChatEnvironmentSettings.WorkingDirectories"/>).
		/// </summary>
		public bool FetchFromAllWorkingDirectories
		{
			get => _fetchFromAllWorkingDirectories;
			set => SetProperty(ref _fetchFromAllWorkingDirectories, value);
		}

		private readonly RangeObservableCollection<string> _additionalSubAgentDirectories = [];
		/// <summary>
		/// Gets or sets the additional sub-agent directories. These are paths similar to <c>agents/</c> directories.
		/// Example for additional directory <c>C:/MyProject/.claude/agents/</c>: <c>C:/MyProject/.claude/agents/my-agent.md</c>
		/// </summary>
		public RangeObservableCollection<string> AdditionalSubAgentDirectories
		{
			get => _additionalSubAgentDirectories;
			set => _additionalSubAgentDirectories.Reset(value);
		}

		private readonly RangeObservableCollection<string> _additionalSubAgentFiles = [];
		/// <summary>
		/// Gets or sets the additional sub-agent files. These are paths to individual sub-agent files.
		/// </summary>
		public RangeObservableCollection<string> AdditionalSubAgentFiles
		{
			get => _additionalSubAgentFiles;
			set => _additionalSubAgentFiles.Reset(value);
		}
	}
}
