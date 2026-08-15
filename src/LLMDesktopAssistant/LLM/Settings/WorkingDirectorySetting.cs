namespace LLMDesktopAssistant.LLM.Settings
{
	public class WorkingDirectorySetting : NotifyPropertyChanged
	{
		private string? _name;
		/// <summary>
		/// The name of the working directory setting.
		/// </summary>
		public string? Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		private string? _path;
		/// <summary>
		/// The path of the working directory setting.
		/// </summary>
		public string? Path
		{
			get => _path;
			set => SetProperty(ref _path, value);
		}

		private bool _isEnabled = true;
		/// <summary>
		/// Whether the working directory is enabled or not. Used for convenience to disable certain settings without removing them.
		/// </summary>
		public bool IsEnabled
		{
			get => _isEnabled;
			set => SetProperty(ref _isEnabled, value);
		}

		private bool _isActive = false;
		/// <summary>
		/// Whether the working directory is currently active or not. Only ONE working directory can be active at a time.
		/// </summary>
		public bool IsActive
		{
			get => _isActive;
			set => SetProperty(ref _isActive, value);
		}
	}
}