using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Prompting.LLT;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.MVVM.Debug;

/// <summary>
/// View model for the LLT editor debug page: hosts an <see cref="LLTEditorControl"/>
/// and shows live parser diagnostics (token count, parse errors, parse time).
/// </summary>
[ViewModelFor(typeof(LLTEditorDebugPageView))]
public class LLTEditorDebugPageViewModel : ViewModelBase
{
	private const string DefaultSample = """
		@template greeting
		{
			Hello, @name!
			@if age > 18 { adult } else { young }
		}

		@template broken
		{
			@if ( {
				this expression is broken
			}
		}

		@template farewell
		{
			Bye, @name!
		}
		""";

	private readonly LLTTokenClassifier _classifier = new();

	private string _text = DefaultSample;
	/// <summary>
	/// Gets or sets the text being edited. Changing it re-runs the parser diagnostics.
	/// </summary>
	public string Text
	{
		get => _text;
		set
		{
			if (SetProperty(ref _text, value))
				UpdateDiagnostics();
		}
	}

	private int _tokenCount;
	/// <summary>
	/// Gets the number of classified tokens in the current text.
	/// </summary>
	public int TokenCount
	{
		get => _tokenCount;
		private set => SetProperty(ref _tokenCount, value);
	}

	private int _errorCount;
	/// <summary>
	/// Gets the number of relevant parse errors in the current text.
	/// </summary>
	public int ErrorCount
	{
		get => _errorCount;
		private set => SetProperty(ref _errorCount, value);
	}

	private double _parseTimeMs;
	/// <summary>
	/// Gets the time spent on the last parse, in milliseconds.
	/// </summary>
	public double ParseTimeMs
	{
		get => _parseTimeMs;
		private set => SetProperty(ref _parseTimeMs, value);
	}

	/// <summary>
	/// Gets the list of parse errors in the current text.
	/// </summary>
	public RangeObservableCollection<LLTParseError> Errors { get; } = [];

	/// <summary>
	/// Gets the command that restores the default sample text.
	/// </summary>
	public IRelayCommand ResetCommand { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="LLTEditorDebugPageViewModel"/> class.
	/// </summary>
	public LLTEditorDebugPageViewModel()
	{
		ResetCommand = new RelayCommand(Reset);
		UpdateDiagnostics();
	}

	private void Reset()
	{
		Text = DefaultSample;
	}

	private void UpdateDiagnostics()
	{
		var stopwatch = Stopwatch.StartNew();
		var (segments, errors) = _classifier.Classify(_text);
		stopwatch.Stop();

		TokenCount = segments.Count(s => s.Kind != LLTTokenKind.Error);
		ErrorCount = errors.Count;
		ParseTimeMs = stopwatch.Elapsed.TotalMilliseconds;

		Errors.Reset(errors);
	}
}
