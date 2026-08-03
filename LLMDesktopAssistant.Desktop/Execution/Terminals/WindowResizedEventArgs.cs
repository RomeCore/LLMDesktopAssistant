using Avalonia.Interactivity;

namespace LLMDesktopAssistant.Desktop.Execution.Terminals
{
	/// <summary>
	/// EventArgs for the WindowResized event.
	/// </summary>
	public class WindowResizedEventArgs : RoutedEventArgs
	{
		public int Width { get; }
		public int Height { get; }

		public WindowResizedEventArgs(int width, int height)
		{
			Width = width;
			Height = height;
		}
	}
}
