using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Desktop.Execution
{
	public class ProcessOutput : NotifyPropertyChanged
	{
		/// <summary>
		/// Standard output lines of the process.
		/// </summary>
		public RangeObservableCollection<string> StdOut { get; } = [];

		/// <summary>
		/// Standard error output of the process.
		/// </summary>
		public RangeObservableCollection<string> StdErr { get; } = [];

		/// <summary>
		/// Standard output and error output of the process combined.
		/// </summary>
		public RangeObservableCollection<string> Output { get; } = [];
	}
}
