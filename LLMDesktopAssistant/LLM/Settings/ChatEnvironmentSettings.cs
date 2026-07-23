using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;
using LLMDesktopAssistant.Tools.Implementations.Filesystem;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Environment and working directory settings.
	/// </summary>
	public class ChatEnvironmentSettings : ChatSettingsCategoryBase
	{
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
		public string GetWorkingDirectory() => WorkingDirectories.FirstOrDefault(w => w.IsEnabled && w.IsActive)?.Path
			?? Path.GetFullPath(Directories.DefaultWorkingDirectory);

		private string? _pythonVenvActivateScriptPath;
		/// <summary>
		/// The path to the script that activates a python virtual environment.
		/// </summary>
		public string? PythonVenvActivateScriptPath
		{
			get => _pythonVenvActivateScriptPath;
			set => SetProperty(ref _pythonVenvActivateScriptPath, value);
		}

		private string? _pythonMetaVenvActivateScriptPath;
		/// <summary>
		/// The path to the script that activates a python virtual environment.
		/// Used for meta-tools. If null, the <see cref="PythonMetaVenvActivateScriptPath"/> will be used.
		/// </summary>
		public string? PythonMetaVenvActivateScriptPath
		{
			get => _pythonMetaVenvActivateScriptPath;
			set => SetProperty(ref _pythonMetaVenvActivateScriptPath, value);
		}
	}
}