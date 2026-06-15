using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LLMDesktopAssistant.LLM.Attachments;
using LLMDesktopAssistant.LLM.MVVM;
using LLMDesktopAssistant.LLM.Services.Attachments;
using ReverseMarkdown.Converters;
using Serilog;

namespace LLMDesktopAssistant.LLM.MVVM;

public partial class UserInputView : UserControl
{
	public UserInputView()
	{
		InitializeComponent();

		InputTextBox.PastingFromClipboard += InputTextBox_PastingFromClipboard;
		InputTextBox.AddHandler(TextBox.KeyDownEvent, InputTextBox_KeyDown, RoutingStrategies.Tunnel);

		DragDrop.SetAllowDrop(this, true);
		AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
		AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
		AddHandler(DragDrop.DropEvent, OnDrop);
	}

	private async void InputTextBox_PastingFromClipboard(object? sender, RoutedEventArgs e)
	{
		if (DataContext is not UserInputViewModel vm)
			return;

		var clipboard = App.MainTopLevel.Clipboard;
		if (clipboard == null)
			return;

		try
		{
			var image = await clipboard.TryGetBitmapAsync();
			if (image != null)
			{
				_ = vm.AcceptImageAsync(image);
				e.Handled = true;
				return;
			}

			var files = await clipboard.TryGetFilesAsync();
			if (files != null && files.Length > 0)
			{
				_ = vm.AcceptFilesAsync(files);
				e.Handled = true;
				return;
			}
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Failed to paste from clipboard: {Error}", ex.Message);
		}
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
						viewModel.SendCurrentUserInputAsync(generate: true);
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