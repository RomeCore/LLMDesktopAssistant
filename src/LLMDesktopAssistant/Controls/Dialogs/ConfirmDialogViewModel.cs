using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.MVVM;

namespace LLMDesktopAssistant.Controls.Dialogs
{
	/// <summary>
	/// ViewModel for a confirmation dialog with confirm and cancel buttons.
	/// The dialog is closed by either button; the result is exposed through <see cref="Result"/>.
	/// </summary>
	[ViewModelFor(typeof(ConfirmDialogView))]
	public class ConfirmDialogViewModel : NotifyPropertyChanged
	{
		private bool _isResultSet;

		private string _title = string.Empty;
		/// <summary>
		/// Gets or sets the title of the dialog.
		/// </summary>
		public string Title
		{
			get => _title;
			set => SetProperty(ref _title, value);
		}

		private string _description = string.Empty;
		/// <summary>
		/// Gets or sets the description shown under the title.
		/// </summary>
		public string Description
		{
			get => _description;
			set => SetProperty(ref _description, value);
		}

		private string _confirmText = "OK";
		/// <summary>
		/// Gets or sets the text of the confirm button.
		/// </summary>
		public string ConfirmText
		{
			get => _confirmText;
			set => SetProperty(ref _confirmText, value);
		}

		private string _cancelText = "Cancel";
		/// <summary>
		/// Gets or sets the text of the cancel button.
		/// </summary>
		public string CancelText
		{
			get => _cancelText;
			set => SetProperty(ref _cancelText, value);
		}

		private bool _isDanger;
		/// <summary>
		/// Gets or sets a value indicating whether the confirm button is styled as a danger action.
		/// </summary>
		public bool IsDanger
		{
			get => _isDanger;
			set => SetProperty(ref _isDanger, value);
		}

		/// <summary>
		/// Gets the command that confirms the action and closes the dialog.
		/// </summary>
		public IRelayCommand ConfirmCommand { get; }

		/// <summary>
		/// Gets the command that cancels the action and closes the dialog.
		/// </summary>
		public IRelayCommand CancelCommand { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConfirmDialogViewModel"/> class.
		/// </summary>
		public ConfirmDialogViewModel()
		{
			ConfirmCommand = new RelayCommand(Confirm, () => !_isResultSet);
			CancelCommand = new RelayCommand(Cancel, () => !_isResultSet);
		}

		private void Confirm()
		{
			if (_isResultSet)
				return;

			_isResultSet = true;
			DialogManager.CloseDialog(true);
		}

		private void Cancel()
		{
			if (_isResultSet)
				return;

			_isResultSet = true;
			DialogManager.CloseDialog(false);
		}
	}
}
