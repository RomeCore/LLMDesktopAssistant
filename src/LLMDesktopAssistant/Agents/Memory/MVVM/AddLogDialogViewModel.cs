using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.MVVM;

namespace LLMDesktopAssistant.Agents.Memory.MVVM
{
	/// <summary>
	/// Result of the <see cref="AddLogDialogViewModel"/>: the entered log text, importance,
	/// real-time window and alternative timeline window.
	/// </summary>
	/// <param name="Text">The text of the log.</param>
	/// <param name="Importance">The importance score of the log, between 0.0 and 1.0.</param>
	/// <param name="TimeStampBegin">The real-time timestamp when the log began.</param>
	/// <param name="TimeStampEnd">The real-time timestamp when the log ended.</param>
	/// <param name="TimeLineOrdinalBegin">The game-time ordinal when the log began.</param>
	/// <param name="TimeLineDetailsBegin">The game-time details when the log began.</param>
	/// <param name="TimeLineOrdinalEnd">The game-time ordinal when the log ended.</param>
	/// <param name="TimeLineDetailsEnd">The game-time details when the log ended.</param>
	public sealed record AddLogDialogResult(
		string Text,
		double Importance,
		DateTime TimeStampBegin,
		DateTime TimeStampEnd,
		double TimeLineOrdinalBegin,
		string TimeLineDetailsBegin,
		double TimeLineOrdinalEnd,
		string TimeLineDetailsEnd);

	/// <summary>
	/// ViewModel for the "Add Log" dialog with a text area, an importance slider and
	/// the real-time and alternative timeline windows. The dialog is closed by either
	/// button; the entered data is exposed through <see cref="Result"/>.
	/// </summary>
	[ViewModelFor(typeof(AddLogDialogView))]
	public class AddLogDialogViewModel : NotifyPropertyChanged
	{
		private readonly TaskCompletionSource<AddLogDialogResult?> _tcs = new();
		private bool _isResultSet;

		/// <summary>
		/// Gets the task that completes with the entered log data, or <see langword="null"/> when cancelled.
		/// </summary>
		public Task<AddLogDialogResult?> Result => _tcs.Task;

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
		/// Gets or sets the text of the log.
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
		/// Gets or sets the placeholder text of the log input.
		/// </summary>
		public string Placeholder
		{
			get => _placeholder;
			set => SetProperty(ref _placeholder, value);
		}

		private double _importance = 0.5;
		/// <summary>
		/// Gets or sets the importance of the log, between 0.0 and 1.0.
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

		private string _beginLabel = string.Empty;
		/// <summary>
		/// Gets or sets the label of the begin timestamp section.
		/// </summary>
		public string BeginLabel
		{
			get => _beginLabel;
			set => SetProperty(ref _beginLabel, value);
		}

		private string _endLabel = string.Empty;
		/// <summary>
		/// Gets or sets the label of the end timestamp section.
		/// </summary>
		public string EndLabel
		{
			get => _endLabel;
			set => SetProperty(ref _endLabel, value);
		}

		private string _timelineLabel = string.Empty;
		/// <summary>
		/// Gets or sets the label of the timeline section.
		/// </summary>
		public string TimelineLabel
		{
			get => _timelineLabel;
			set => SetProperty(ref _timelineLabel, value);
		}

		private string _ordinalLabel = string.Empty;
		/// <summary>
		/// Gets or sets the label of the timeline ordinal field.
		/// </summary>
		public string OrdinalLabel
		{
			get => _ordinalLabel;
			set => SetProperty(ref _ordinalLabel, value);
		}

		private string _detailsLabel = string.Empty;
		/// <summary>
		/// Gets or sets the label of the timeline details field.
		/// </summary>
		public string DetailsLabel
		{
			get => _detailsLabel;
			set => SetProperty(ref _detailsLabel, value);
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

		// Real-time window (defaults to now).

		private DateTimeOffset? _beginDate = DateTimeOffset.Now;
		/// <summary>
		/// Gets or sets the date part of the begin timestamp.
		/// </summary>
		public DateTimeOffset? BeginDate
		{
			get => _beginDate;
			set => SetProperty(ref _beginDate, value);
		}

		private TimeSpan? _beginTime = DateTimeOffset.Now.TimeOfDay;
		/// <summary>
		/// Gets or sets the time part of the begin timestamp.
		/// </summary>
		public TimeSpan? BeginTime
		{
			get => _beginTime;
			set => SetProperty(ref _beginTime, value);
		}

		private DateTimeOffset? _endDate = DateTimeOffset.Now;
		/// <summary>
		/// Gets or sets the date part of the end timestamp.
		/// </summary>
		public DateTimeOffset? EndDate
		{
			get => _endDate;
			set => SetProperty(ref _endDate, value);
		}

		private TimeSpan? _endTime = DateTimeOffset.Now.TimeOfDay;
		/// <summary>
		/// Gets or sets the time part of the end timestamp.
		/// </summary>
		public TimeSpan? EndTime
		{
			get => _endTime;
			set => SetProperty(ref _endTime, value);
		}

		// Alternative timeline window (defaults to zero / empty).

		private decimal? _timeLineOrdinalBegin;
		/// <summary>
		/// Gets or sets the game-time ordinal when the log began.
		/// </summary>
		public decimal? TimeLineOrdinalBegin
		{
			get => _timeLineOrdinalBegin;
			set => SetProperty(ref _timeLineOrdinalBegin, value);
		}

		private string _timeLineDetailsBegin = string.Empty;
		/// <summary>
		/// Gets or sets the game-time details when the log began.
		/// </summary>
		public string TimeLineDetailsBegin
		{
			get => _timeLineDetailsBegin;
			set => SetProperty(ref _timeLineDetailsBegin, value);
		}

		private decimal? _timeLineOrdinalEnd;
		/// <summary>
		/// Gets or sets the game-time ordinal when the log ended.
		/// </summary>
		public decimal? TimeLineOrdinalEnd
		{
			get => _timeLineOrdinalEnd;
			set => SetProperty(ref _timeLineOrdinalEnd, value);
		}

		private string _timeLineDetailsEnd = string.Empty;
		/// <summary>
		/// Gets or sets the game-time details when the log ended.
		/// </summary>
		public string TimeLineDetailsEnd
		{
			get => _timeLineDetailsEnd;
			set => SetProperty(ref _timeLineDetailsEnd, value);
		}

		/// <summary>
		/// Gets the command that submits the entered log and closes the dialog.
		/// </summary>
		public IRelayCommand SubmitCommand { get; }

		/// <summary>
		/// Gets the command that cancels the dialog.
		/// </summary>
		public IRelayCommand CancelCommand { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="AddLogDialogViewModel"/> class.
		/// </summary>
		public AddLogDialogViewModel()
		{
			SubmitCommand = new RelayCommand(Submit, () => !_isResultSet && !string.IsNullOrWhiteSpace(Text));
			CancelCommand = new RelayCommand(Cancel, () => !_isResultSet);
		}

		private void Submit()
		{
			if (_isResultSet || string.IsNullOrWhiteSpace(Text))
				return;

			var result = new AddLogDialogResult(
				Text.Trim(),
				Math.Clamp(Importance, 0, 1),
				Combine(BeginDate, BeginTime) ?? DateTime.Now,
				Combine(EndDate, EndTime) ?? DateTime.Now,
				(double)(TimeLineOrdinalBegin ?? 0),
				TimeLineDetailsBegin.Trim(),
				(double)(TimeLineOrdinalEnd ?? 0),
				TimeLineDetailsEnd.Trim());

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

		private static DateTime? Combine(DateTimeOffset? date, TimeSpan? time)
		{
			if (date == null && time == null)
				return null;

			var day = date?.Date ?? DateTime.Today;
			return day + (time ?? TimeSpan.Zero);
		}
	}
}
