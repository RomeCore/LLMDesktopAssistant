using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.MVVM;

namespace LLMDesktopAssistant.Agents.Memory.MVVM
{
	/// <summary>
	/// Result of the <see cref="AddFactDialogViewModel"/>: the entered fact text and its importance.
	/// </summary>
	/// <param name="Text">The text of the fact.</param>
	/// <param name="Importance">The importance score of the fact, between 0.0 and 1.0.</param>
	public sealed record AddFactDialogResult(string Text, double Importance);

	/// <summary>
	/// ViewModel for the "Add Fact" dialog with a text area and an importance slider.
	/// The dialog is closed by either button; the entered data is exposed through <see cref="Result"/>.
	/// </summary>
	[ViewModelFor(typeof(AddFactDialogView))]
	public class AddFactDialogViewModel : NotifyPropertyChanged
	{
		private readonly TaskCompletionSource<AddFactDialogResult?> _tcs = new();
		private bool _isResultSet;

		/// <summary>
		/// Gets the task that completes with the entered fact data, or <see langword="null"/> when cancelled.
		/// </summary>
		public Task<AddFactDialogResult?> Result => _tcs.Task;

		private string _title = string.Empty;
		/// <summary>
		/// Gets or sets the title of the dialog.
		/// </summary>
		public string Title
		{
			get => _title;
			set => SetProperty(ref _title, value);
		}

		private string _text = string.Empty;
		/// <summary>
		/// Gets or sets the text of the fact.
		/// </summary>
		public string Text
		{
			get => _text;
			set
			{
				if (SetProperty(ref _text, value))
					SubmitCommand.NotifyCanExecuteChanged();
			}
		}

		private string _placeholder = string.Empty;
		/// <summary>
		/// Gets or sets the placeholder text of the fact input.
		/// </summary>
		public string Placeholder
		{
			get => _placeholder;
			set => SetProperty(ref _placeholder, value);
		}

		private double _importance = 0.7;
		/// <summary>
		/// Gets or sets the importance of the fact, between 0.0 and 1.0.
		/// </summary>
		public double Importance
		{
			get => _importance;
			set => SetProperty(ref _importance, value);
		}

		private string _importanceLabel = string.Empty;
		/// <summary>
		/// Gets or sets the label of the importance slider.
		/// </summary>
		public string ImportanceLabel
		{
			get => _importanceLabel;
			set => SetProperty(ref _importanceLabel, value);
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
		/// Gets the command that submits the entered fact and closes the dialog.
		/// </summary>
		public IRelayCommand SubmitCommand { get; }

		/// <summary>
		/// Gets the command that cancels the dialog.
		/// </summary>
		public IRelayCommand CancelCommand { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="AddFactDialogViewModel"/> class.
		/// </summary>
		public AddFactDialogViewModel()
		{
			SubmitCommand = new RelayCommand(Submit, () => !_isResultSet && !string.IsNullOrWhiteSpace(Text));
			CancelCommand = new RelayCommand(Cancel, () => !_isResultSet);
		}

		private void Submit()
		{
			if (_isResultSet || string.IsNullOrWhiteSpace(Text))
				return;

			var result = new AddFactDialogResult(Text.Trim(), Math.Clamp(Importance, 0, 1));
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
