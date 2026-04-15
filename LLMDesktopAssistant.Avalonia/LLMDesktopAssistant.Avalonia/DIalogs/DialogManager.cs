using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Avalonia.DIalogs
{
	public static class DialogManager
	{
		private static readonly Stack<FATaskDialog> _dialogStack = [];

		private static IList<FATaskDialogButton> ResolveButtons(DialogButttons buttons)
		{
			var result = new List<FATaskDialogButton>();

			if (buttons.HasFlag(DialogButttons.Ok))
				result.Add(FATaskDialogButton.OKButton);
			if (buttons.HasFlag(DialogButttons.Cancel))
				result.Add(FATaskDialogButton.CancelButton);

			return result;
		}

		/// <summary>
		/// Displays a dialog asynchronously.
		/// </summary>
		/// <param name="viewModel">The view model to display in the dialog.</param>
		/// <param name="cancellationToken">The cancellation token that will used to close the dialog.</param>
		/// <returns></returns>
		public static async Task<object?> ShowDialogAsync(object? viewModel, DialogButttons buttons = DialogButttons.Ok,
			CancellationToken cancellationToken = default)
		{
			if (cancellationToken.IsCancellationRequested)
				return null;

			var dialog = new FATaskDialog
			{
				Content = ViewLocator.Resolve(viewModel),
				XamlRoot = App.MainTopLevel,
				Buttons = ResolveButtons(buttons)
			};

			_dialogStack.Push(dialog);

			try
			{
				return await dialog.ShowAsync(true);
			}
			finally
			{
				if (_dialogStack.TryPeek(out var topDialog) && topDialog == dialog)
					_dialogStack.Pop();
			}
		}

		public static void CloseDialog(object? result = null)
		{
			if (_dialogStack.TryPeek(out var topDialog))
			{
				_dialogStack.Pop();
				topDialog.Hide(result);
			}
		}
	}
}