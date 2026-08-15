using Avalonia.Interactivity;

namespace LLMDesktopAssistant.Desktop.Execution.Terminals
{
	/// <summary>
	/// EventArgs for the TitleChanged event.
	/// </summary>
	public class TitleChangedEventArgs : RoutedEventArgs
	{
		public string Title { get; }

		public TitleChangedEventArgs(string title)
		{
			Title = title;
		}
	}
}
