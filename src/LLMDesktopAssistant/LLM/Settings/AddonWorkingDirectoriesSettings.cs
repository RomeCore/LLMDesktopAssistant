using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Represents the working directories group of the addon settings: the flag controlling
	/// whether addons are fetched from all working directories and the flag controlling whether
	/// working directories are used as addon packs themselves.
	/// </summary>
	public class AddonWorkingDirectoriesSettings : NotifyPropertyChanged
	{
		private bool _fetchFromAllWorkingDirectories;
		/// <summary>
		/// Gets or sets a value indicating whether addons are fetched from all enabled working directories.
		/// </summary>
		public bool FetchFromAllWorkingDirectories
		{
			get => _fetchFromAllWorkingDirectories;
			set => SetProperty(ref _fetchFromAllWorkingDirectories, value);
		}

		private bool _useWorkingDirectoriesAsPacks;
		/// <summary>
		/// Gets or sets a value indicating whether each working directory is used as an addon pack itself.
		/// </summary>
		public bool UseWorkingDirectoriesAsPacks
		{
			get => _useWorkingDirectoriesAsPacks;
			set => SetProperty(ref _useWorkingDirectoriesAsPacks, value);
		}
	}
}
