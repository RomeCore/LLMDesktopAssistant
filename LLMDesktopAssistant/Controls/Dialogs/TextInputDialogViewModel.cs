using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.MVVM;

namespace LLMDesktopAssistant.Controls.Dialogs
{
	/// <summary>
	/// ViewModel for a text input dialog with submit and cancel buttons.
	/// The dialog is closed by either button; the entered text is exposed through <see cref="Result"/>.
	/// </summary>
	[ViewModelFor(typeof(TextInputDialogView))]
	public class TextInputDialogViewModel : NotifyPropertyChanged
	{
		private readonly TaskCompletionSource<string?> _tcs = new();
		private bool _isResultSet;

		/// <summary>
		/// Gets the task that completes with the entered text, or <see langword="null"/> when cancelled.
		/// </summary>
		public Task<string?> Result => _tcs.Task;

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

		private string _label = string.Empty;
		/// <summary>
		/// Gets or sets the label of the input field.
		/// </summary>
		public string Label
		{
			get => _label;
			set => SetProperty(ref _label, value);
		}

		private string _placeholder = string.Empty;
		/// <summary>
		/// Gets or sets the placeholder text of the input field.
		/// </summary>
		public string Placeholder
		{
			get => _placeholder;
			set => SetProperty(ref _placeholder, value);
		}

		private string _value = string.Empty;
		/// <summary>
		/// Gets or sets the current value of the input field.
		/// </summary>
		public string Value
		{
			get => _value;
			set
			{
				if (SetProperty(ref _value, value))
					SubmitCommand.NotifyCanExecuteChanged();
			}
		}

		private bool _isMultiline;
		/// <summary>
		/// Gets or sets a value indicating whether the input field is a multiline text area.
		/// </summary>
		public bool IsMultiline
		{
			get => _isMultiline;
			set => SetProperty(ref _isMultiline, value);
		}

		private bool _isRequired;
		/// <summary>
		/// Gets or sets a value indicating whether non-empty input is required for submission.
		/// </summary>
		public bool IsRequired
		{
			get => _isRequired;
			set
			{
				if (SetProperty(ref _isRequired, value))
					SubmitCommand.NotifyCanExecuteChanged();
			}
		}

		private string _submitText = "OK";
		/// <summary>
		/// Gets or sets the text of the submit button.
		/// </summary>
		public string SubmitText
		{
			get => _submitText;
			set => SetProperty(ref _submitText, value);
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

		/// <summary>
		/// Gets the command that submits the entered text and closes the dialog.
		/// </summary>
		public IRelayCommand SubmitCommand { get; }

		/// <summary>
		/// Gets the command that cancels the dialog.
		/// </summary>
		public IRelayCommand CancelCommand { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TextInputDialogViewModel"/> class.
		/// </summary>
		public TextInputDialogViewModel()
		{
			SubmitCommand = new RelayCommand(Submit, () => !_isResultSet && (!IsRequired || !string.IsNullOrWhiteSpace(Value)));
			CancelCommand = new RelayCommand(Cancel, () => !_isResultSet);
		}

		private void Submit()
		{
			if (_isResultSet || (IsRequired && string.IsNullOrWhiteSpace(Value)))
				return;

			var result = Value.Trim();
			_isResultSet = true;
			_tcs.TrySetResult(result);
			DialogManager.CloseDialog(result);
		}

		private void Cancel()
		{
			if (_isResultSet)
				return;

			_isResultSet = true;
			_tcs.TrySetResult(null);
			DialogManager.CloseDialog(null);
		}
	}
}
