using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Controls.Dialogs
{
	public static class DialogManager
	{
		/// <summary>
		/// Displays a dialog asynchronously.
		/// </summary>
		/// <param name="viewModel">The view model to display in the dialog.</param>
		/// <returns></returns>
		public static async Task<object?> ShowDialogAsync(object? viewModel, DialogButttons buttons = DialogButttons.Ok)
		{
			var dvm = new DialogViewModel
			{
				Content = viewModel
			};
			var dv = new DialogView { DataContext = dvm };
			return await DialogHostAvalonia.DialogHost.Show(dv);
		}
	}
}