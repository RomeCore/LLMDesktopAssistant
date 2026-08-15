using Avalonia.Controls;
using Avalonia.Input;

namespace LLMDesktopAssistant.LLM.Messages;

public partial class ToolCallView : UserControl
{
	public ToolCallView()
	{
		InitializeComponent();
	}

	private void NotesTextBox_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key != Key.Enter ||
			sender is not TextBox tb ||
			DataContext is not ToolCallViewModel vm)
			return;

		App.MainTopLevel.FocusManager.Focus(null);
		vm.CommitNotes(tb.Text);
	}
}
