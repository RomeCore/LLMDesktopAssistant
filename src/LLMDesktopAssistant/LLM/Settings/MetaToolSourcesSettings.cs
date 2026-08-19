using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Represents the meta tool sources group of a chat: the flag controlling whether meta tools
	/// are fetched from all working directories, and the lists of additional meta tool
	/// directories and files.
	/// </summary>
	public class MetaToolSourcesSettings : NotifyPropertyChanged
	{
		private bool _fetchFromAllWorkingDirectories = false;
		/// <summary>
		/// Gets or sets a value indicating whether meta tools should be fetched from all
		/// working directories (see <see cref="ChatEnvironmentSettings.WorkingDirectories"/>).
		/// </summary>
		public bool FetchFromAllWorkingDirectories
		{
			get => _fetchFromAllWorkingDirectories;
			set => SetProperty(ref _fetchFromAllWorkingDirectories, value);
		}

		private readonly RangeObservableCollection<string> _additionalMetaToolDirectories = [];
		/// <summary>
		/// Gets or sets the additional meta tool directories. These are paths to directories
		/// containing meta tool script files.
		/// </summary>
		public RangeObservableCollection<string> AdditionalMetaToolDirectories
		{
			get => _additionalMetaToolDirectories;
			set => _additionalMetaToolDirectories.Reset(value);
		}

		private readonly RangeObservableCollection<string> _additionalMetaToolFiles = [];
		/// <summary>
		/// Gets or sets the additional meta tool files. These are paths to individual meta tool script files.
		/// </summary>
		public RangeObservableCollection<string> AdditionalMetaToolFiles
		{
			get => _additionalMetaToolFiles;
			set => _additionalMetaToolFiles.Reset(value);
		}
	}
}
