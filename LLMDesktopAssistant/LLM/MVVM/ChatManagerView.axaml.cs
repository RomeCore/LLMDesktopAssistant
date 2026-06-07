using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace LLMDesktopAssistant.LLM.MVVM;

public partial class ChatManagerView : UserControl
{
	public ChatManagerView()
	{
		InitializeComponent();
	}

	private void TabTitleTextBlock_PointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (sender is TextBlock textBlock && textBlock.DataContext is OpenedChatViewModel vm)
		{
			vm.StartEditTitleCommand.Execute(null);
		}
	}

	private void TabTitleTextBox_KeyDown(object? sender, KeyEventArgs e)
	{
		if (sender is TextBox textBox && textBox.DataContext is OpenedChatViewModel vm)
		{
			if (e.Key == Key.Enter)
			{
				vm.CommitEditTitleCommand.Execute(null);
				e.Handled = true;
			}
			else if (e.Key == Key.Escape)
			{
				vm.CancelEditTitleCommand.Execute(null);
				e.Handled = true;
			}
		}
	}

	private void TabTopicTextBlock_PointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (sender is TextBlock textBlock && textBlock.DataContext is OpenedChatViewModel vm)
		{
			vm.StartEditTopicCommand.Execute(null);
		}
	}

	private void TabTopicTextBox_KeyDown(object? sender, KeyEventArgs e)
	{
		if (sender is TextBox textBox && textBox.DataContext is OpenedChatViewModel vm)
		{
			if (e.Key == Key.Enter)
			{
				vm.CommitEditTopicCommand.Execute(null);
				e.Handled = true;
			}
			else if (e.Key == Key.Escape)
			{
				vm.CancelEditTopicCommand.Execute(null);
				e.Handled = true;
			}
		}
	}
}
