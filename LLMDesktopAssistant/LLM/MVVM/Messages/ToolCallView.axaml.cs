using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace LLMDesktopAssistant.LLM.Messages;

public partial class ToolCallView : UserControl
{
	public ToolCallView()
	{
		InitializeComponent();
	}

	private void ReasonTextBox_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key != Key.Enter ||
			sender is not TextBox tb ||
			DataContext is not ToolCallViewModel vm)
			return;

		App.MainTopLevel.FocusManager.Focus(null);
		vm.CancelWithReasonEndCommand.Execute(tb.Text);
	}
}
