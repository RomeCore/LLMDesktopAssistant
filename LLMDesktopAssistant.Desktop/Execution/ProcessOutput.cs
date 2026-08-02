namespace LLMDesktopAssistant.Desktop.Execution
{
	public class ProcessOutput : NotifyPropertyChanged
	{
		private string? _stdOut;
		/// <summary>
		/// Standard output of the process.
		/// </summary>
		public string? StdOut
		{
			get => _stdOut;
			internal set => SetProperty(ref _stdOut, value);
		}

		private string? _stdErr;
		/// <summary>
		/// Standard error output of the process.
		/// </summary>
		public string? StdErr
		{
			get => _stdErr;
			internal set => SetProperty(ref _stdErr, value);
		}
	}
}
