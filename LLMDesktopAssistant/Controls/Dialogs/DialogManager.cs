using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Controls.Dialogs
{
	public static class DialogManager
	{
		private static readonly Stack<DialogViewModel> _dialogStack = [];

		/// <summary>
		/// Gets whether any dialogs are currently open.
		/// </summary>
		public static bool IsDialogOpen => _dialogStack.Count > 0;

		/// <summary>
		/// Displays a dialog asynchronously.
		/// </summary>
		/// <param name="viewModel">The view model to display in the dialog.</param>
		/// <returns></returns>
		public static async Task<object?> ShowDialogAsync(object? viewModel)
		{
			var dvm = new DialogViewModel
			{
				Content = viewModel
			};
			_dialogStack.Push(dvm);

			try
			{
				var dv = new DialogView { DataContext = dvm };
				return await DialogHostAvalonia.DialogHost.Show(dv, dialogIdentifier: null);
			}
			finally
			{
				_dialogStack.Pop();
			}
		}

		/// <summary>
		/// Closes the currently open dialog.
		/// </summary>
		/// <param name="result">The result to pass back to the dialog's view model.</param>
		public static void CloseDialog(object? result = null)
		{
			DialogHostAvalonia.DialogHost.Close(dialogIdentifier: null, parameter: result);
		}
	}
}