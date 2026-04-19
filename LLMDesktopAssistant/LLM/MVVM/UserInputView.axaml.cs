using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LLMDesktopAssistant.LLM;
using LLMDesktopAssistant.LLM.Attachments;
using ReverseMarkdown.Converters;

namespace LLMDesktopAssistant.LLM;

public partial class UserInputView : UserControl
{
	public UserInputView()
	{
		InitializeComponent();

		InputTextBox.AddHandler(TextBox.KeyDownEvent, InputTextBox_KeyDown, RoutingStrategies.Tunnel);

		DragDrop.SetAllowDrop(this, true);
		AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
		AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
		AddHandler(DragDrop.DropEvent, OnDrop);
	}

	private void InputTextBox_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.Enter)
		{
			if (DataContext is UserInputViewModel viewModel)
			{
				if (e.KeyModifiers == KeyModifiers.None)
				{
					if (viewModel.IsGenerating)
						viewModel.CancelGenerationCommand.Execute(null);
					else if (!string.IsNullOrWhiteSpace(viewModel.Text))
						viewModel.SendCurrentUserInputAsync();
					e.Handled = true;
				}
			}
		}
	}

	private void OnDragEnter(object? sender, DragEventArgs e)
	{
		if (e.DataTransfer.Contains(DataFormat.File) ||
			e.DataTransfer.Contains(DataFormat.Text))
		{
			e.DragEffects = DragDropEffects.Copy;
			DropOverlay.IsVisible = true;
		}
		else
		{
			e.DragEffects = DragDropEffects.None;
		}

		e.Handled = true;
	}

	private void OnDragLeave(object? sender, DragEventArgs e)
	{
		DropOverlay.IsVisible = false;
	}

	private void OnDrop(object? sender, DragEventArgs e)
	{
		DropOverlay.IsVisible = false;

		if (DataContext is UserInputViewModel vm)
		{
			_ = vm.AcceptDropAsync(e);
		}

		e.Handled = true;
	}
}