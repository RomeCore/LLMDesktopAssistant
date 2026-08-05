using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Represents the working directory configuration for a chat: the default working directory
	/// flags and the list of additional working directories.
	/// </summary>
	public class WorkingDirectoriesSettings : NotifyPropertyChanged
	{
		private bool _isDefaultWorkingDirectoryEnabled = true;
		/// <summary>
		/// Gets or sets a value indicating whether the default working directory (<see cref="Directories.DefaultWorkingDirectory"/>) is enabled.
		/// </summary>
		public bool IsDefaultWorkingDirectoryEnabled
		{
			get => _isDefaultWorkingDirectoryEnabled;
			set => SetProperty(ref _isDefaultWorkingDirectoryEnabled, value);
		}

		private bool _isDefaultWorkingDirectoryActive = true;
		/// <summary>
		/// Gets or sets a value indicating whether the default working directory (<see cref="Directories.DefaultWorkingDirectory"/>) is active.
		/// </summary>
		public bool IsDefaultWorkingDirectoryActive
		{
			get => _isDefaultWorkingDirectoryActive;
			set => SetProperty(ref _isDefaultWorkingDirectoryActive, value);
		}

		private readonly RangeObservableCollection<WorkingDirectorySetting> _items = [];
		/// <summary>
		/// The list of additional working directories that can be used by the agent.
		/// </summary>
		public RangeObservableCollection<WorkingDirectorySetting> Items
		{
			get => _items;
			set => _items.Reset(value);
		}

		/// <summary>
		/// Returns the working directory for the chatbot. If no working directory is specified, returns the default directory.
		/// </summary>
		public string GetWorkingDirectory() => IsDefaultWorkingDirectoryActive ? Directories.DefaultWorkingDirectory :
			Items.FirstOrDefault(w => w.IsEnabled && w.IsActive)?.Path
			?? Directories.DefaultWorkingDirectory;

		/// <summary>
		/// Returns a list of enabled working directories. If the default directory is enabled, it is included in the list.
		/// </summary>
		/// <returns>A list of enabled working directories.</returns>
		public List<string> GetEnabledWorkingDirectories()
		{
			var result = new List<string>();
			if (IsDefaultWorkingDirectoryEnabled)
				result.Add(Directories.DefaultWorkingDirectory);
			foreach (var wd in Items)
				if (wd.IsEnabled && !string.IsNullOrEmpty(wd.Path))
					result.Add(wd.Path);
			return result;
		}
	}
}
