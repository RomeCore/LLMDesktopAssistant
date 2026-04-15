using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LLMDesktopAssistant.Avalonia.LLM.Messages;

public partial class ToolCallView : UserControl
{
	public ToolCallView()
	{
		InitializeComponent();
	}

	private void ToolPopupButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
	{
		ToolPopup.IsOpen = true;
	}
}