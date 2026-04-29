using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Environment and working directory settings.
	/// </summary>
	public class ChatEnvironmentSettings : NotifyPropertyChanged
	{
		private string? _workingDirectory;
		/// <summary>
		/// The working directory for the chatbot. This can be used to store files and execute commands, python scripts etc.
		/// </summary>
		public string? WorkingDirectory
		{
			get => _workingDirectory;
			set => SetProperty(ref _workingDirectory, value);
		}

		/// <summary>
		/// Returns the working directory for the chatbot. If no working directory is specified, returns the default directory.
		/// </summary>
		public string GetWorkingDirectory() => WorkingDirectory ?? Path.GetFullPath(Directories.DefaultWorkingDirectory);

		private string? _pythonVenvActivateScriptPath;
		/// <summary>
		/// The path to the script that activates a python virtual environment.

		private int _maxVisibleRounds = 0;
		/// <summary>
		/// The maximum number of recent conversation rounds (user message group + assistant message group) visible to agents.
		/// 0 means no limit — all rounds are visible (subject to other permissions).
		/// </summary>
		public int MaxVisibleRounds
		{
			get => _maxVisibleRounds;
			set => SetProperty(ref _maxVisibleRounds, Math.Max(0, value));
		}

		/// </summary>
		public string? PythonVenvActivateScriptPath
		{
			get => _pythonVenvActivateScriptPath;
			set => SetProperty(ref _pythonVenvActivateScriptPath, value);
		}
	}
}