using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Environment and working directory settings.
	/// </summary>
	public class ChatEnvironmentSettings : ChatSettingsCategoryBase
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

		private readonly RangeObservableCollection<WorkingDirectorySetting> _workingDirectories = [];
		/// <summary>
		/// The list of working directories that can be used by the agent.
		/// </summary>
		public RangeObservableCollection<WorkingDirectorySetting> WorkingDirectories
		{
			get => _workingDirectories;
			set => _workingDirectories.Reset(value);
		}

		private readonly RangeObservableCollection<DirectoryAccessSetting> _directoryAccessRules = [];
		/// <summary>
		/// The list of directory access rules.
		/// </summary>
		public RangeObservableCollection<DirectoryAccessSetting> DirectoryAccessRules
		{
			get => _directoryAccessRules;
			set => _directoryAccessRules.Reset(value);
		}

		/// <summary>
		/// Returns the working directory for the chatbot. If no working directory is specified, returns the default directory.
		/// </summary>
		public string GetWorkingDirectory() => IsDefaultWorkingDirectoryActive ? Directories.DefaultWorkingDirectory :
			WorkingDirectories.FirstOrDefault(w => w.IsEnabled && w.IsActive)?.Path
			?? Directories.DefaultWorkingDirectory;

		private readonly RangeObservableCollection<AdditionalEnvironmentSetting> _additionalSettings = [];
		/// <summary>
		/// The list of additional environment settings.
		/// </summary>
		public RangeObservableCollection<AdditionalEnvironmentSetting> AdditionalSettings
		{
			get => _additionalSettings;
			set => _additionalSettings.Reset(value);
		}

		/// <summary>
		/// Returns an additional environment setting of type <typeparamref name="T"/>. If no such setting exists, a new one is created and added to the collection.
		/// </summary>
		/// <typeparam name="T">The type of the additional environment setting. Must inherit from <see cref="AdditionalEnvironmentSetting"/> and have a parameterless constructor.</typeparam>
		/// <returns>The additional environment setting of type <typeparamref name="T"/>.</returns>
		public T EnsureAdditional<T>() where T : AdditionalEnvironmentSetting, new()
		{
			if (AdditionalSettings.FirstOrDefault(s => s is T) is T found) return found;
			found = new();
			AdditionalSettings.Add(found);
			return found;
		}
	}
}