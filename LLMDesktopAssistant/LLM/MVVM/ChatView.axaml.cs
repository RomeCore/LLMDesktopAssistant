using Avalonia;
using Avalonia.Controls;

namespace LLMDesktopAssistant.LLM.MVVM;

public partial class ChatView : UserControl
{
	public ChatView()
	{
		InitializeComponent();
	}

	private void MessagesScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
	{
		ScrollToBottomButton.IsVisible = MessagesScrollViewer.Offset.Y < MessagesScrollViewer.Extent.Height - MessagesScrollViewer.Viewport.Height;
	}

	private void ScrollToBottomButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
	{
		MessagesScrollViewer.SetCurrentValue(ScrollViewer.OffsetProperty, new Vector(MessagesScrollViewer.Offset.X, double.PositiveInfinity));
	}
}